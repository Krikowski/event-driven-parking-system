using Estapar.Parking.Domain.Entities;

namespace Estapar.Parking.Application.Abstractions.Persistence;

public interface IVehicleEventRepository
{
    Task<bool> ExistsByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        VehicleEvent vehicleEvent,
        CancellationToken cancellationToken = default);
}