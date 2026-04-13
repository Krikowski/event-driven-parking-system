using Estapar.Parking.Application.UseCases.Garage;
using Microsoft.Extensions.Logging;

namespace Estapar.Parking.Api.HostedServices;

public sealed class GarageBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GarageBootstrapHostedService> _logger;

    public GarageBootstrapHostedService(
        IServiceProvider serviceProvider,
        ILogger<GarageBootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting garage configuration bootstrap.");

        using var scope = _serviceProvider.CreateScope();

        var syncUseCase = scope.ServiceProvider
            .GetRequiredService<ISyncGarageConfigurationUseCase>();

        await syncUseCase.ExecuteAsync(cancellationToken);

        _logger.LogInformation("Garage configuration bootstrap completed successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}