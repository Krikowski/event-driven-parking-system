using System.Text.Json;

using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;

namespace Estapar.Parking.Application.Common.Webhooks;

public static class VehicleEventFactory
{
    public static VehicleEvent Create(
        string idempotencyKey,
        ParkingEventType eventType,
        string normalizedLicensePlate,
        object payload)
    {
        var payloadSnapshot = JsonSerializer.Serialize(payload);

        return new VehicleEvent(
            idempotencyKey,
            eventType,
            normalizedLicensePlate,
            payloadSnapshot,
            DateTime.UtcNow);
    }
}