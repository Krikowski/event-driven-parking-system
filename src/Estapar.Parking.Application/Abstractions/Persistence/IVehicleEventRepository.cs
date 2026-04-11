using Estapar.Parking.Domain.Entities;

namespace Estapar.Parking.Application.Abstractions.Persistence;

public interface IVehicleEventRepository
{
    Task AddAsync(VehicleEvent vehicleEvent, CancellationToken cancellationToken = default);
}