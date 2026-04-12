using Estapar.Parking.Application.UseCases.Garage;

namespace Estapar.Parking.Api.HostedServices;

public sealed class GarageBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public GarageBootstrapHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var syncUseCase = scope.ServiceProvider
            .GetRequiredService<ISyncGarageConfigurationUseCase>();

        await syncUseCase.ExecuteAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}