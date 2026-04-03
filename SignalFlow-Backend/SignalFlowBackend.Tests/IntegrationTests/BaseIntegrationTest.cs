using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SignalFlow.Backend.IntegrationTests;

public abstract class BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();
    protected IntegrationTestWebAppFactory Factory { get; } = factory;
    protected IServiceProvider Services => _scope.ServiceProvider;

    public void Dispose()
    {
        _scope.Dispose();
    }
}