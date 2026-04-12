using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Domain.Enums;
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

        return await _dbContext.ParkingSessions
            .AsNoTracking()
            .Where(session =>
                session.SectorCode == sectorCode &&
                session.Status == ParkingSessionStatus.Closed &&
                session.ExitTimeUtc.HasValue &&
                session.ExitTimeUtc.Value >= startOfDayUtc &&
                session.ExitTimeUtc.Value < endOfDayUtc)
            .SumAsync(session => session.ChargedAmount ?? 0m, cancellationToken);
    }
}