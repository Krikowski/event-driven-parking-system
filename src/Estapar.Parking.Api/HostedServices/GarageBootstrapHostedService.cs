using Estapar.Parking.Application.UseCases.Garage;

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

        try
        {
            using var scope = _serviceProvider.CreateScope();

            var syncUseCase = scope.ServiceProvider
                .GetRequiredService<ISyncGarageConfigurationUseCase>();

            await syncUseCase.ExecuteAsync(cancellationToken);

            _logger.LogInformation("Garage configuration bootstrap completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Garage configuration bootstrap failed. Application startup will be aborted.");

            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
