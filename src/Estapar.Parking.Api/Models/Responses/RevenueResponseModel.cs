namespace Estapar.Parking.Api.Models.Responses;

public sealed class RevenueResponseModel
{
    public decimal Amount { get; init; }

    public string Currency { get; init; } = default!;

    public DateTime Timestamp { get; init; }
}