namespace Estapar.Parking.Application.Contracts.Webhooks;

public sealed record HandleParkedEventCommand(
    string LicensePlate,
    decimal Latitude,
    decimal Longitude);