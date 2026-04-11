using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.UnitTests.Domain.Entities;

public class ParkingSessionTests
{
    [Fact]
    public void Constructor_ShouldNormalizeLicensePlate()
    {
        var session = new ParkingSession(" abc1234 ", "A", CreateUtcDate(12, 0, 0), 10m);

        Assert.Equal("ABC1234", session.LicensePlate);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenEntryTimeIsNotUtc()
    {
        Action act = () => new ParkingSession(
            "ABC1234",
            "A",
            new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local),
            10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Entry time must be informed in UTC.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenFrozenHourlyRateIsNegative()
    {
        Action act = () => new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), -1m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Frozen hourly rate cannot be negative.", exception.Message);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenSessionIsAlreadyClosed()
    {
        var session = new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);

        session.Close(CreateUtcDate(13, 0, 0), 10m);

        Action act = () => session.Close(CreateUtcDate(14, 0, 0), 20m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking session is already closed.", exception.Message);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenExitTimeIsEarlierThanEntryTime()
    {
        var session = new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);

        Action act = () => session.Close(CreateUtcDate(11, 59, 59), 10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Exit time cannot be earlier than entry time.", exception.Message);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenExitTimeIsNotUtc()
    {
        var session = new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);

        Action act = () => session.Close(
            new DateTime(2025, 1, 1, 13, 0, 0, DateTimeKind.Local),
            10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Exit time must be informed in UTC.", exception.Message);
    }

    [Fact]
    public void Close_ShouldSetExitTimeChargedAmountAndClosedStatus()
    {
        var exitTimeUtc = CreateUtcDate(13, 0, 0);
        var session = new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);

        session.Close(exitTimeUtc, 10m);

        Assert.Equal(exitTimeUtc, session.ExitTimeUtc);
        Assert.Equal(10m, session.ChargedAmount);
        Assert.Equal(ParkingSessionStatus.Closed, session.Status);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSessionAlreadyHasAnAssignedSpot()
    {
        var session = new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);

        session.AssignParkingSpot(1, "A");

        Action act = () => session.AssignParkingSpot(2, "A");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking session already has an assigned spot.", exception.Message);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSpotSectorDoesNotMatchSessionSector()
    {
        var session = new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);

        Action act = () => session.AssignParkingSpot(1, "B");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking spot sector does not match session sector.", exception.Message);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSessionIsClosed()
    {
        var session = new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);
        session.Close(CreateUtcDate(13, 0, 0), 10m);

        Action act = () => session.AssignParkingSpot(1, "A");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Cannot assign a parking spot to a closed session.", exception.Message);
    }

    [Fact]
    public void AssignParkingSpot_ShouldAssignSpotId_WhenSessionIsActiveAndSpotBelongsToSameSector()
    {
        var session = new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);

        session.AssignParkingSpot(1, " a ");

        Assert.Equal(1, session.ParkingSpotId);
        Assert.True(session.HasAssignedSpot);
    }

    private static DateTime CreateUtcDate(int hour, int minute, int second)
    {
        return new DateTime(2025, 1, 1, hour, minute, second, DateTimeKind.Utc);
    }
}
