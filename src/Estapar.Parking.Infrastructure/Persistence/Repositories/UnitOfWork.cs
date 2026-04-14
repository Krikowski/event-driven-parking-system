using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Infrastructure.Persistence.Exceptions;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ParkingDbContext _dbContext;

    public UnitOfWork(ParkingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (TryMapConflict(ex, out var conflictType))
        {
            throw new PersistenceConflictException(conflictType, ex);
        }
    }

    private static bool TryMapConflict(
        DbUpdateException exception,
        out PersistenceConflictType conflictType)
    {
        var rawMessage = exception.InnerException?.Message ?? exception.Message;

        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            conflictType = PersistenceConflictType.UnknownUniqueConstraint;
            return false;
        }

        if (!IsUniqueConstraintViolation(exception, rawMessage))
        {
            conflictType = PersistenceConflictType.UnknownUniqueConstraint;
            return false;
        }

        if (rawMessage.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase) ||
            rawMessage.Contains("IX_VehicleEvents_IdempotencyKey", StringComparison.OrdinalIgnoreCase))
        {
            conflictType = PersistenceConflictType.DuplicateWebhookEvent;
            return true;
        }

        if (rawMessage.Contains("IX_ParkingSessions_ActiveLicensePlate", StringComparison.OrdinalIgnoreCase) ||
            rawMessage.Contains("ParkingSessions.LicensePlate", StringComparison.OrdinalIgnoreCase))
        {
            conflictType = PersistenceConflictType.ActiveSessionAlreadyExists;
            return true;
        }

        if (rawMessage.Contains("IX_ParkingSessions_ActiveParkingSpot", StringComparison.OrdinalIgnoreCase) ||
            rawMessage.Contains("ParkingSessions.ParkingSpotId", StringComparison.OrdinalIgnoreCase))
        {
            conflictType = PersistenceConflictType.ParkingSpotAlreadyAssigned;
            return true;
        }

        conflictType = PersistenceConflictType.UnknownUniqueConstraint;
        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception, string rawMessage)
    {
        if (exception.InnerException is SqlException sqlException &&
            (sqlException.Number == 2601 || sqlException.Number == 2627))
        {
            return true;
        }

        return rawMessage.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
               rawMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               rawMessage.Contains("unique index", StringComparison.OrdinalIgnoreCase) ||
               rawMessage.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
