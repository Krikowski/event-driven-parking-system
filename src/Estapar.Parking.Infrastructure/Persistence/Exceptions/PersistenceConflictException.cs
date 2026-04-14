namespace Estapar.Parking.Infrastructure.Persistence.Exceptions;

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictType ConflictType { get; }

    public PersistenceConflictException(
        PersistenceConflictType conflictType,
        Exception? innerException = null)
        : base($"Persistence conflict: {conflictType}.", innerException)
    {
        ConflictType = conflictType;
    }
}
