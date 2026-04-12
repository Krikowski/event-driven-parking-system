namespace Estapar.Parking.Application.UseCases.Entry;

public interface IHandleEntryEventUseCase
{
    Task ExecuteAsync(
        Contracts.Webhooks.HandleEntryEventCommand command,
        CancellationToken cancellationToken = default);
}