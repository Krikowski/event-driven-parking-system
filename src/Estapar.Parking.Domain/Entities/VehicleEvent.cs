using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Domain.Entities;

public class VehicleEvent
{
    public string IdempotencyKey { get; }
    public ParkingEventType EventType { get; }
    public string LicensePlate { get; }
    public string PayloadSnapshot { get; }
    public DateTime ProcessedAtUtc { get; }

    public VehicleEvent(
        string idempotencyKey,
        ParkingEventType eventType,
        string licensePlate,
        string payloadSnapshot,
        DateTime processedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new DomainException("Idempotency key is required.");
        }

        if (!Enum.IsDefined(typeof(ParkingEventType), eventType))
        {
            throw new DomainException("Parking event type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new DomainException("License plate is required.");
        }

        if (string.IsNullOrWhiteSpace(payloadSnapshot))
        {
            throw new DomainException("Payload snapshot is required.");
        }

        if (processedAtUtc == default)
        {
            throw new DomainException("Processed timestamp is required.");
        }

        if (processedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Processed timestamp must be informed in UTC.");
        }

        IdempotencyKey = idempotencyKey.Trim();
        EventType = eventType;
        LicensePlate = licensePlate.Trim().ToUpperInvariant();
        PayloadSnapshot = payloadSnapshot.Trim();
        ProcessedAtUtc = processedAtUtc;
    }
}