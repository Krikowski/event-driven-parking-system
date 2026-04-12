namespace Estapar.Parking.Application.Contracts.Revenue;

public sealed record RevenueResultDto(
    decimal Amount,
    string Currency,
    DateTime GeneratedAtUtc);