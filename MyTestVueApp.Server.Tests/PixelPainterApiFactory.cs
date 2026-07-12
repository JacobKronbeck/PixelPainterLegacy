using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyTestVueApp.Server.Auth;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyTestVueApp.Server.Tests
{
    public class PixelPainterApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:16-3.4")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public string ConnectionString => _postgres.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await ApplyMigrationsAsync();
        }

        public new async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApplicationConfiguration:ConnectionString"] = ConnectionString,
                    ["ApplicationConfiguration:ClientId"] = "test-client",
                    ["ApplicationConfiguration:ClientSecret"] = "test-secret",
                    ["ApplicationConfiguration:RedirectUrl"] = "http://localhost:5173",
                    ["ApplicationConfiguration:PostLoginRedirectUrl"] = "http://localhost:5173"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IGoogleOAuthClient, FakeGoogleOAuthClient>();
            });
        }

        private async Task ApplyMigrationsAsync()
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            foreach (var file in Directory.GetFiles(GetMigrationsPath(), "*.sql").OrderBy(path => path))
            {
                await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(file), connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        private static string GetMigrationsPath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "supabase", "migrations");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate supabase/migrations.");
        }
    }
}
