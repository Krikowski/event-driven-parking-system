using System.Text.Json;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;

namespace Estapar.Parking.Application.Common.Webhooks;

public static class VehicleEventFactory
{
    public static VehicleEvent Create(
        ParkingEventType eventType,
        string normalizedLicensePlate,
        object payload)
    {
        var payloadSnapshot = JsonSerializer.Serialize(payload);

        return new VehicleEvent(
            eventType,
            normalizedLicensePlate,
            payloadSnapshot,
            DateTime.UtcNow);
    }
}