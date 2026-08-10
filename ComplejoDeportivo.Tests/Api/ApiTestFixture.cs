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
using Microsoft.AspNetCore.Hosting;
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
                // Explicit, not relied-on-by-default: Program.cs skips the rate limiter middleware
                // specifically when the environment is "Testing", since this fixture's helpers
                // register/log in far more than the rate limiter would otherwise allow in one run.
                builder.UseEnvironment("Testing");
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
            // /api/account/register-empleado now requires an existing Admin token (see
            // AccountController) precisely to prevent unauthenticated self-registration as staff --
            // so bootstrapping the very first Admin for tests has to go around the HTTP API and
            // seed the Empleado/Usuario rows directly, the same way a real deployment would via a
            // one-time DB seed script rather than a public endpoint.
            const string email = "admin2@test.com";
            const string password = "Password123!";

            var optionsBuilder = new DbContextOptionsBuilder<ComplejoDeportivoContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            using (var context = new ComplejoDeportivoContext(optionsBuilder.Options))
            {
                var existing = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
                if (existing == null)
                {
                    var empleado = new Empleado { Nombre = "Admin", Apellido = "Test", Cargo = "Admin", FechaIngreso = DateOnly.FromDateTime(DateTime.UtcNow) };
                    context.Empleados.Add(empleado);
                    await context.SaveChangesAsync();

                    context.Usuarios.Add(new Usuario
                    {
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        TipoUsuario = "Empleado",
                        EmpleadoId = empleado.EmpleadoId,
                        FechaRegistro = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }

            var loginDto = new LoginRequestDTO { Email = email, Password = password };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

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
