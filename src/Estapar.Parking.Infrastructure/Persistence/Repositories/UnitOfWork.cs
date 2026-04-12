using Estapar.Parking.Application.Abstractions.Persistence;

namespace Estapar.Parking.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ParkingDbContext _dbContext;

    public UnitOfWork(ParkingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}