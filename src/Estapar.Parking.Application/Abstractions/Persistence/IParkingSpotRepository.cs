using Estapar.Parking.Domain.Entities;

namespace Estapar.Parking.Application.Abstractions.Persistence;

public interface IParkingSpotRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<ParkingSpot?> GetByIdAsync(int parkingSpotId, CancellationToken cancellationToken = default);

    Task<ParkingSpot?> GetByCoordinatesAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

    Task<ParkingSpot?> GetFirstAvailableBySectorCodeAsync(string sectorCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ParkingSpot>> GetBySectorCodeAsync(string sectorCode, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<ParkingSpot> parkingSpots, CancellationToken cancellationToken = default);
}
