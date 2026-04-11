namespace Estapar.Parking.Application.Abstractions.Persistence;

public interface IRevenueReadRepository
{
    Task<decimal> GetRevenueAmountAsync(string sectorCode, DateOnly date, CancellationToken cancellationToken = default);
}