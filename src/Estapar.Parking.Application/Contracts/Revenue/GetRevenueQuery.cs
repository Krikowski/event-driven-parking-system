namespace Estapar.Parking.Application.Contracts.Revenue;

public sealed record GetRevenueQuery(
    string SectorCode,
    DateOnly Date);