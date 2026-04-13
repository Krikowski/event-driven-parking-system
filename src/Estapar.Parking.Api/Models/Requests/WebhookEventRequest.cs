namespace Estapar.Parking.Api.Models.Requests;

public sealed class WebhookEventRequest
{
    public string EventType { get; init; } = default!;
    public string LicensePlate { get; init; } = default!;
    public DateTime? EntryTime { get; init; }
    public DateTime? ExitTime { get; init; }
    public decimal? Lat { get; init; }
    public decimal? Lng { get; init; }
}