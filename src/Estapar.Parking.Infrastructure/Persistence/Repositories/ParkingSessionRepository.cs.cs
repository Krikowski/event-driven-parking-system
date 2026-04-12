using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.Infrastructure.Persistence.Repositories;

public sealed class ParkingSessionRepository : IParkingSessionRepository
{
    private readonly ParkingDbContext _dbContext;

    public ParkingSessionRepository(ParkingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsActiveByLicensePlateAsync(
        string licensePlate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSessions
            .AnyAsync(
                session => session.LicensePlate == licensePlate &&
                           session.Status == ParkingSessionStatus.Active,
                cancellationToken);
    }

    public Task<ParkingSession?> GetActiveByLicensePlateAsync(
        string licensePlate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSessions
            .FirstOrDefaultAsync(
                session => session.LicensePlate == licensePlate &&
                           session.Status == ParkingSessionStatus.Active,
                cancellationToken);
    }

    public Task AddAsync(ParkingSession parkingSession, CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSessions.AddAsync(parkingSession, cancellationToken).AsTask();
    }
}