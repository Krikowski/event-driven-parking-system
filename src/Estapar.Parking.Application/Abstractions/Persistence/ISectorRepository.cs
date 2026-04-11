using Estapar.Parking.Domain.Entities;

namespace Estapar.Parking.Application.Abstractions.Persistence;

public interface ISectorRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<Sector?> GetByCodeAsync(string sectorCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Sector>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Sector> sectors, CancellationToken cancellationToken = default);
}