using Estapar.Parking.Domain.Entities;

namespace Estapar.Parking.Application.Abstractions.Persistence;

public interface IParkingSessionRepository
{
    Task<bool> ExistsActiveByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default);

    Task<ParkingSession?> GetActiveByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default);

    Task AddAsync(ParkingSession parkingSession, CancellationToken cancellationToken = default);
}