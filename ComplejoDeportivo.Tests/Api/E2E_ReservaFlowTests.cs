using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Api;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    public class E2E_ReservaFlowTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly MsSqlContainer _dbContainer;
        private HttpClient _client = null!;

        public E2E_ReservaFlowTests(WebApplicationFactory<Program> factory)
        {
            _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            // Resolve relative to the test assembly's own output directory
            // (ComplejoDeportivo.Tests/bin/Debug/net10.0 -> repo root -> database/create-database.sql),
            // not Directory.GetCurrentDirectory() which varies depending on how the test host is launched.
            var scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../database/create-database.sql"));
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Database script not found at {scriptPath}");
            }

            var script = await File.ReadAllTextAsync(scriptPath);
            // The script hardcodes a Windows data-file path from the original dev machine;
            // replace it with a real path inside the Linux-based SQL Server container.
            script = script.Replace(@"C:\Users\marti\ComplejoDeportivo.mdf", "/var/opt/mssql/data/ComplejoDeportivo.mdf")
                            .Replace(@"C:\Users\marti\ComplejoDeportivo_log.ldf", "/var/opt/mssql/data/ComplejoDeportivo_log.ldf");
            var execResult = await _dbContainer.ExecScriptAsync(script);
            if (execResult.ExitCode != 0)
            {
                throw new Exception($"Failed to execute DB script: {execResult.Stderr}");
            }

            // The schema script creates and populates the "ComplejoDeportivo" database, but the
            // container's default connection string targets "master" — every context here must
            // explicitly point at "ComplejoDeportivo" or it'll see an empty database.
            var connectionString = _dbContainer.GetConnectionString();
            connectionString = connectionString.Contains("Database=master")
                ? connectionString.Replace("Database=master", "Database=ComplejoDeportivo")
                : connectionString + ";Database=ComplejoDeportivo";

            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ComplejoDeportivoContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ComplejoDeportivoContext>(options =>
                    {
                        options.UseSqlServer(connectionString);
                    });
                });
            }).CreateClient();

            await SeedDatabaseAsync(connectionString);
        }

        private async Task SeedDatabaseAsync(string connectionString)
        {
            using var scope = _factory.Services.CreateScope();
            // Need a fresh context connected to the testcontainer
            var optionsBuilder = new DbContextOptionsBuilder<ComplejoDeportivoContext>();
            optionsBuilder.UseSqlServer(connectionString);
            using var context = new ComplejoDeportivoContext(optionsBuilder.Options);

            // Insert reference data
            var estadoConfirmada = new EstadoReserva { Nombre = "Confirmada" };
            var estadoCancelada = new EstadoReserva { Nombre = "Cancelada" };
            var estadoPendiente = new EstadoReserva { Nombre = "Pendiente" };
            context.EstadoReservas.AddRange(estadoConfirmada, estadoCancelada, estadoPendiente);
            await context.SaveChangesAsync();

            var direccion = new Direccion { Calle = "Falsa", Numero = "123", Ciudad = "Springfield", Provincia = "SP", CodigoPostal = "28001" };
            context.Direccions.Add(direccion);
            await context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Complejo Test", DireccionId = direccion.DireccionId };
            context.Complejos.Add(complejo);
            await context.SaveChangesAsync();

            var tipoCancha = new TipoCancha { Nombre = "Futbol 5" };
            context.TipoCanchas.Add(tipoCancha);

            var tipoSuperficie = new TipoSuperficie { Nombre = "Sintetico" };
            context.TipoSuperficies.Add(tipoSuperficie);
            await context.SaveChangesAsync();

            var cancha = new Cancha { Nombre = "Cancha 1", ComplejoId = complejo.ComplejoId, TipoCanchaId = tipoCancha.TipoCanchaId, TipoSuperficieId = tipoSuperficie.TipoSuperficieId, Activa = true };
            context.Canchas.Add(cancha);
            await context.SaveChangesAsync();

            var tarifa = new Tarifa { CanchaId = cancha.CanchaId, Precio = 1000, FechaVigencia = new DateOnly(2020, 1, 1), EsActual = true };
            context.Tarifas.Add(tarifa);
            await context.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await _dbContainer.DisposeAsync();
        }

        [Fact]
        public async Task FullReservaFlow_ShouldSucceed()
        {
            // 1. Register Client
            var registerDto = new RegisterClienteDTO
            {
                Email = "testclient@test.com",
                Password = "Password123!",
                Nombre = "Test",
                Apellido = "Client",
                Telefono = "123456789",
                Documento = "12345678"
            };
            var registerResponse = await _client.PostAsJsonAsync("/api/account/register", registerDto);
            registerResponse.EnsureSuccessStatusCode();

            // 2. Login
            var loginDto = new LoginRequestDTO
            {
                Email = "testclient@test.com",
                Password = "Password123!"
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
            
            loginResult.Should().NotBeNull();
            loginResult!.Token.Should().NotBeNullOrEmpty();
            loginResult.ClienteId.Should().BeGreaterThan(0);
            int clienteId = loginResult.ClienteId!.Value;

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

            // 3. Check availability
            // Two days out (not "tomorrow"), fixed at 18:00: cancellation later in this same test
            // requires >=24h of notice, and "tomorrow at a fixed hour" is only >24h away half the
            // time depending on what hour the suite happens to run — was a flaky, time-of-day-
            // dependent failure waiting to happen with AddDays(1).
            var fechaReserva = DateOnly.FromDateTime(DateTime.Now.AddDays(2)).ToString("yyyy-MM-dd");
            var availabilityResponse = await _client.GetAsync($"/api/reserva/disponibilidad?canchaId=1&fecha={fechaReserva}");
            availabilityResponse.EnsureSuccessStatusCode();

            // 4. Create reservation
            var createReservaDto = new CrearReservaDTO
            {
                ClienteId = clienteId,
                CanchaIds = new List<int> { 1 },
                Fecha = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                HoraInicio = new TimeOnly(18, 0),
                HoraFin = new TimeOnly(19, 0),
                Ambito = "Web"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/reserva", createReservaDto);
            createResponse.EnsureSuccessStatusCode();

            // 5. List reservations
            var listResponse = await _client.GetAsync($"/api/reserva/cliente/{clienteId}");
            listResponse.EnsureSuccessStatusCode();
            var reservations = await listResponse.Content.ReadFromJsonAsync<List<ReservaDTO>>();
            reservations.Should().NotBeNull();
            reservations.Should().ContainSingle();
            var reservaId = reservations![0].ReservaId;

            // 6. Cancel reservation
            var cancelDto = new CancelarReservaDTO
            {
                ReservaId = reservaId,
                ClienteId = clienteId,
                Motivo = "Test cancellation"
            };
            var cancelResponse = await _client.PutAsJsonAsync("/api/reserva/cancelar", cancelDto);
            cancelResponse.EnsureSuccessStatusCode();

            // 7. Verify it is cancelled
            var listResponseAfter = await _client.GetAsync($"/api/reserva/cliente/{clienteId}");
            var reservationsAfter = await listResponseAfter.Content.ReadFromJsonAsync<List<ReservaDTO>>();
            reservationsAfter![0].Estado.Should().Be("Cancelada");
        }
    }
}
