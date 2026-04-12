namespace Estapar.Parking.Application.UseCases.Garage;

public interface ISyncGarageConfigurationUseCase
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}