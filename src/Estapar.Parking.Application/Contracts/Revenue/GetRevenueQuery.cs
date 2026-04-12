namespace Estapar.Parking.Application.UseCases.Revenue;

public sealed record GetRevenueQuery(
    string SectorCode,
    DateOnly Date);