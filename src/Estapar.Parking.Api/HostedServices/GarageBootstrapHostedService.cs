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
        } catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Garage configuration bootstrap failed. The application will continue running, but garage data may be unavailable until the issue is resolved.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}