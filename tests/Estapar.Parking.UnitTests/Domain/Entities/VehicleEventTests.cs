using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

using Xunit;

namespace Estapar.Parking.UnitTests.Domain.Entities;

public class VehicleEventTests
{
    [Fact]
    public void Constructor_ShouldNormalizeLicensePlate()
    {
        var vehicleEvent = new VehicleEvent(
            "KEY123",
            ParkingEventType.Entry,
            " abc1234 ",
            "{ \"event_type\": \"ENTRY\" }",
            CreateUtcDate());

        Assert.Equal("ABC1234", vehicleEvent.LicensePlate);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenIdempotencyKeyIsEmpty()
    {
        Action act = () => new VehicleEvent(
            " ",
            ParkingEventType.Entry,
            "ABC1234",
            "{ \"event_type\": \"ENTRY\" }",
            CreateUtcDate());

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Idempotency key is required.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenEventTypeIsInvalid()
    {
        Action act = () => new VehicleEvent(
            "KEY123",
            (ParkingEventType)999,
            "ABC1234",
            "{ \"event_type\": \"ENTRY\" }",
            CreateUtcDate());

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking event type is invalid.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenLicensePlateIsEmpty()
    {
        Action act = () => new VehicleEvent(
            "KEY123",
            ParkingEventType.Entry,
            " ",
            "{ \"event_type\": \"ENTRY\" }",
            CreateUtcDate());

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("License plate is required.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenPayloadSnapshotIsEmpty()
    {
        Action act = () => new VehicleEvent(
            "KEY123",
            ParkingEventType.Entry,
            "ABC1234",
            " ",
            CreateUtcDate());

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Payload snapshot is required.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenProcessedAtIsDefault()
    {
        Action act = () => new VehicleEvent(
            "KEY123",
            ParkingEventType.Entry,
            "ABC1234",
            "{ \"event_type\": \"ENTRY\" }",
            default);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Processed timestamp is required.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenProcessedAtIsNotUtc()
    {
        Action act = () => new VehicleEvent(
            "KEY123",
            ParkingEventType.Entry,
            "ABC1234",
            "{ \"event_type\": \"ENTRY\" }",
            new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local));

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Processed timestamp must be informed in UTC.", exception.Message);
    }

    private static DateTime CreateUtcDate()
    {
        return new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }
}