namespace Estapar.Parking.Application.Contracts.Webhooks;

public sealed record HandleEntryEventCommand(
    string LicensePlate,
    DateTime EntryTimeUtc);