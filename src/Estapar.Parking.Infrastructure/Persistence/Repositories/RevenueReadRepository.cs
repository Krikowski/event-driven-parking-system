using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.Infrastructure.Persistence.Repositories;

public sealed class RevenueReadRepository : IRevenueReadRepository
{
    private readonly ParkingDbContext _dbContext;

    public RevenueReadRepository(ParkingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetRevenueAmountAsync(
        string sectorCode,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var startOfDayUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDayUtc = startOfDayUtc.AddDays(1);

        var chargedAmounts = await _dbContext.Set<Domain.Entities.ParkingSession>()
            .AsNoTracking()
            .Where(parkingSession =>
                parkingSession.SectorCode == sectorCode &&
                parkingSession.ExitTimeUtc.HasValue &&
                parkingSession.ExitTimeUtc.Value >= startOfDayUtc &&
                parkingSession.ExitTimeUtc.Value < endOfDayUtc &&
                parkingSession.ChargedAmount.HasValue)
            .Select(parkingSession => parkingSession.ChargedAmount!.Value)
            .ToListAsync(cancellationToken);

        return chargedAmounts.Sum();
    }
}