using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Domain.Entities;

namespace Estapar.Parking.Infrastructure.Persistence.Repositories;

public sealed class VehicleEventRepository : IVehicleEventRepository
{
    private readonly ParkingDbContext _dbContext;

    public VehicleEventRepository(ParkingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(VehicleEvent vehicleEvent, CancellationToken cancellationToken = default)
    {
        return _dbContext.VehicleEvents.AddAsync(vehicleEvent, cancellationToken).AsTask();
    }
}