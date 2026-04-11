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
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new DomainException("License plate is required.");
        }

        if (string.IsNullOrWhiteSpace(payloadSnapshot))
        {
            throw new DomainException("Payload snapshot is required.");
        }

        LicensePlate = licensePlate.Trim().ToUpperInvariant();
        EventType = eventType;
        PayloadSnapshot = payloadSnapshot;
        ProcessedAtUtc = processedAtUtc;
    }
}