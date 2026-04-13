using System.Text.Json.Serialization;

namespace Estapar.Parking.Api.Models.Requests;

public sealed class WebhookEventRequest
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = default!;

    [JsonPropertyName("license_plate")]
    public string LicensePlate { get; init; } = default!;

    [JsonPropertyName("entry_time")]
    public DateTime? EntryTime { get; init; }

    [JsonPropertyName("exit_time")]
    public DateTime? ExitTime { get; init; }

    [JsonPropertyName("lat")]
    public decimal? Lat { get; init; }

    [JsonPropertyName("lng")]
    public decimal? Lng { get; init; }
}