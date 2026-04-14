namespace Estapar.Parking.Api.Models.Responses;

public sealed class HealthResponseModel
{
    public string Status { get; init; } = default!;

    public string Service { get; init; } = default!;

    public DateTime Timestamp { get; init; }
}
