namespace Estapar.Parking.Infrastructure.Persistence.Exceptions;

public enum PersistenceConflictType
{
    DuplicateWebhookEvent,
    ActiveSessionAlreadyExists,
    ParkingSpotAlreadyAssigned,
    UnknownUniqueConstraint
}
