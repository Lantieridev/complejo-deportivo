using Testcontainers.MsSql;
using Microsoft.EntityFrameworkCore;
using ComplejoDeportivo.Infrastructure.Persistence;
using System.IO;
using System.Threading.Tasks;
using System;
using Xunit;

namespace ComplejoDeportivo.Tests.Infrastructure.Repositories
{
    public class DatabaseFixture : IAsyncLifetime
    {
        public MsSqlContainer Container { get; private set; } = default!;
        public DbContextOptions<ComplejoDeportivoContext> Options { get; private set; } = default!;
        public string ConnectionString { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            Container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
            await Container.StartAsync();

            // Resolve the path to the script relative to the test assembly output directory
            // (ComplejoDeportivo.Tests/bin/Debug/net10.0 -> repo root -> database/create-database.sql)
            var scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../database/create-database.sql"));
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Database script not found at {scriptPath}");
            }

            var script = await File.ReadAllTextAsync(scriptPath);
            // Replace hardcoded Windows paths with Linux container paths
            script = script.Replace(@"C:\Users\marti\ComplejoDeportivo.mdf", @"/var/opt/mssql/data/ComplejoDeportivo.mdf")
                           .Replace(@"C:\Users\marti\ComplejoDeportivo_log.ldf", @"/var/opt/mssql/data/ComplejoDeportivo_log.ldf");

            var execResult = await Container.ExecScriptAsync(script);
            if (execResult.ExitCode != 0)
            {
                throw new Exception($"Script execution failed: {execResult.Stderr} \n {execResult.Stdout}");
            }

            ConnectionString = Container.GetConnectionString();
            if (ConnectionString.Contains("Database=master"))
            {
                ConnectionString = ConnectionString.Replace("Database=master", "Database=ComplejoDeportivo");
            }
            else
            {
                ConnectionString += ";Database=ComplejoDeportivo";
            }
            ConnectionString += ";TrustServerCertificate=True";

            Options = new DbContextOptionsBuilder<ComplejoDeportivoContext>()
                .UseSqlServer(ConnectionString)
                .Options;
        }

        public async Task DisposeAsync()
        {
            if (Container != null)
            {
                await Container.DisposeAsync();
            }
        }
    }
}
