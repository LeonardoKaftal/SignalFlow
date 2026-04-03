using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalFlowBackend.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SignalFlow.Backend.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer
        = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("signalflow")
            .WithName($"signalflow-tests-{Guid.NewGuid():N}")
            .WithCleanUp(true)
            .WithUsername("signalflow")
            .WithPassword("signalflow123")
            .Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services
                .SingleOrDefault(s => 
                    s.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options
                    .UseNpgsql(_dbContainer.GetConnectionString())
                    .UseSnakeCaseNamingConvention();
            });
        });
    }


    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public new ValueTask DisposeAsync()
    {
        return _dbContainer.DisposeAsync();
    }
}