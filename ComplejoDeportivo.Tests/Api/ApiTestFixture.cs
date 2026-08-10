using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Api;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    public class ApiTestFixture : IAsyncLifetime
    {
        public WebApplicationFactory<Program> Factory { get; private set; } = default!;
        public MsSqlContainer DbContainer { get; private set; } = default!;
        public HttpClient Client { get; private set; } = default!;
        public string ConnectionString { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            DbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await DbContainer.StartAsync();

            var scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../database/create-database.sql"));
            var script = await File.ReadAllTextAsync(scriptPath);
            script = script.Replace(@"C:\Users\marti\ComplejoDeportivo.mdf", "/var/opt/mssql/data/ComplejoDeportivo.mdf")
                            .Replace(@"C:\Users\marti\ComplejoDeportivo_log.ldf", "/var/opt/mssql/data/ComplejoDeportivo_log.ldf");
            var execResult = await DbContainer.ExecScriptAsync(script);
            if (execResult.ExitCode != 0) throw new Exception(execResult.Stderr);

            ConnectionString = DbContainer.GetConnectionString();
            ConnectionString = ConnectionString.Contains("Database=master")
                ? ConnectionString.Replace("Database=master", "Database=ComplejoDeportivo")
                : ConnectionString + ";Database=ComplejoDeportivo";

            Factory = new WebApplicationFactory<Program>();
            Client = Factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ComplejoDeportivoContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ComplejoDeportivoContext>(options =>
                    {
                        options.UseSqlServer(ConnectionString);
                    });
                });
            }).CreateClient();
            
            await SeedDatabaseAsync(ConnectionString);
        }

        private async Task SeedDatabaseAsync(string connectionString)
        {
            using var scope = Factory.Services.CreateScope();
            var optionsBuilder = new DbContextOptionsBuilder<ComplejoDeportivoContext>();
            optionsBuilder.UseSqlServer(connectionString);
            using var context = new ComplejoDeportivoContext(optionsBuilder.Options);
            
            // Reference data
            var estadoConfirmada = new EstadoReserva { Nombre = "Confirmada" };
            var estadoCancelada = new EstadoReserva { Nombre = "Cancelada" };
            var estadoPendiente = new EstadoReserva { Nombre = "Pendiente" };
            context.EstadoReservas.AddRange(estadoConfirmada, estadoCancelada, estadoPendiente);

            var tc = new TipoCancha { Nombre = "TC1" };
            context.TipoCanchas.Add(tc);
            var ts = new TipoSuperficie { Nombre = "TS1" };
            context.TipoSuperficies.Add(ts);
            
            await context.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await DbContainer.DisposeAsync();
            Factory?.Dispose();
        }
        
        public async Task<string> GetAdminTokenAsync()
        {
            var registerDto = new RegisterClienteDTO
            {
                Email = "admin2@test.com",
                Password = "Password123!",
                Nombre = "Admin",
                Apellido = "Test",
                Telefono = "999",
                Documento = "999"
            };
            await Client.PostAsJsonAsync("/api/account/register-empleado", registerDto);

            var loginDto = new LoginRequestDTO { Email = "admin2@test.com", Password = "Password123!" };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
            
            // Upgrade user to Admin
            var optionsBuilder = new DbContextOptionsBuilder<ComplejoDeportivoContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            using var context = new ComplejoDeportivoContext(optionsBuilder.Options);
            
            var user = await context.Usuarios.Include(u => u.Empleado).FirstOrDefaultAsync(u => u.Email == "admin2@test.com");
            if (user != null && user.Empleado != null)
            {
                user.Empleado.Cargo = "Admin";
                await context.SaveChangesAsync();
            }

            // Re-login to get token with Admin role
            loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
            
            return result!.Token;
        }

        public async Task<string> GetClientTokenAsync()
        {
            var registerDto = new RegisterClienteDTO
            {
                Email = "client2@test.com",
                Password = "Password123!",
                Nombre = "Client",
                Apellido = "Test",
                Telefono = "1234",
                Documento = "1234"
            };
            await Client.PostAsJsonAsync("/api/account/register", registerDto);

            var loginDto = new LoginRequestDTO { Email = "client2@test.com", Password = "Password123!" };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
            return result!.Token;
        }
    }

    [CollectionDefinition("ApiTestCollection")]
    public class ApiTestCollection : ICollectionFixture<ApiTestFixture>
    {
    }
}
