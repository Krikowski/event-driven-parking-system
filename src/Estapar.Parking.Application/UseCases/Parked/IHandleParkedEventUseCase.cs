using Estapar.Parking.Application.Contracts.Webhooks;

namespace Estapar.Parking.Application.UseCases.Parked;

public interface IHandleParkedEventUseCase
{
    Task ExecuteAsync(
        HandleParkedEventCommand command,
        CancellationToken cancellationToken = default);
}