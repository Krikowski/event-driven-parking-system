namespace Estapar.Parking.Api.Models.Responses;

public sealed class RevenueResponseModel
{
    public string Sector { get; init; } = default!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "BRL";
    public DateTime GeneratedAt { get; init; }
}