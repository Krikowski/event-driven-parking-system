using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Domain.Entities;

public class VehicleEvent
{
    public ParkingEventType EventType { get; }
    public string LicensePlate { get; }
    public string PayloadSnapshot { get; }
    public DateTime ProcessedAtUtc { get; }

    public VehicleEvent(
        ParkingEventType eventType,
        string licensePlate,
        string payloadSnapshot,
        DateTime processedAtUtc)
    {
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

        EventType = eventType;
        LicensePlate = licensePlate.Trim().ToUpperInvariant();
        PayloadSnapshot = payloadSnapshot;
        ProcessedAtUtc = processedAtUtc;
    }
}