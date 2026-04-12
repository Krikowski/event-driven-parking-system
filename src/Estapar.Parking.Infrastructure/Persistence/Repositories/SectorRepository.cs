using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.Infrastructure.Persistence.Repositories;

public sealed class SectorRepository : ISectorRepository
{
    private readonly ParkingDbContext _dbContext;

    public SectorRepository(ParkingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors.AnyAsync(cancellationToken);
    }

    public Task<Sector?> GetByCodeAsync(string sectorCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors
            .FirstOrDefaultAsync(sector => sector.Code == sectorCode, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sectors
            .OrderBy(sector => sector.Code)
            .ToListAsync(cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<Sector> sectors, CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors.AddRangeAsync(sectors, cancellationToken);
    }
}