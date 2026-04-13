using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.Infrastructure.Persistence.Repositories;

public sealed class VehicleEventRepository : IVehicleEventRepository
{
    private readonly ParkingDbContext _dbContext;

    public VehicleEventRepository(ParkingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.VehicleEvents
            .AnyAsync(vehicleEvent => vehicleEvent.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public Task AddAsync(
        VehicleEvent vehicleEvent,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.VehicleEvents
            .AddAsync(vehicleEvent, cancellationToken)
            .AsTask();
    }
}