using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.Infrastructure.Persistence.Repositories;

public sealed class ParkingSpotRepository : IParkingSpotRepository
{
    private readonly ParkingDbContext _dbContext;

    public ParkingSpotRepository(ParkingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSpots.AnyAsync(cancellationToken);
    }

    public Task<ParkingSpot?> GetByIdAsync(
        int parkingSpotId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSpots
            .FirstOrDefaultAsync(
                parkingSpot => parkingSpot.Id == parkingSpotId,
                cancellationToken);
    }

    public Task<ParkingSpot?> GetByCoordinatesAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSpots
            .FirstOrDefaultAsync(
                parkingSpot => parkingSpot.Latitude == latitude && parkingSpot.Longitude == longitude,
                cancellationToken);
    }

    public Task<ParkingSpot?> GetFirstAvailableBySectorCodeAsync(
        string sectorCode,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSpots
            .Where(parkingSpot => parkingSpot.SectorCode == sectorCode && !parkingSpot.IsOccupied)
            .OrderBy(parkingSpot => parkingSpot.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParkingSpot>> GetBySectorCodeAsync(
        string sectorCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ParkingSpots
            .Where(parkingSpot => parkingSpot.SectorCode == sectorCode)
            .OrderBy(parkingSpot => parkingSpot.Id)
            .ToListAsync(cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<ParkingSpot> parkingSpots, CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSpots.AddRangeAsync(parkingSpots, cancellationToken);
    }
}
