using Estapar.Parking.Application.Contracts.Webhooks;

namespace Estapar.Parking.Application.UseCases.Exit;

public interface IHandleExitEventUseCase
{
    Task ExecuteAsync(
        HandleExitEventCommand command,
        CancellationToken cancellationToken = default);
}