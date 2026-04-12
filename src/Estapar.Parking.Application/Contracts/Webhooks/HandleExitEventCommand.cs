namespace Estapar.Parking.Application.Contracts.Webhooks;

public sealed record HandleExitEventCommand(
    string LicensePlate,
    DateTime ExitTimeUtc);