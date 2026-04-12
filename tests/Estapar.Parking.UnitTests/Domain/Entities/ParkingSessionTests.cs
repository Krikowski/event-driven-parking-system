using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Exceptions;
using Estapar.Parking.Domain.Enums;

namespace Estapar.Parking.UnitTests.Domain.Entities;

public class ParkingSessionTests
{
    [Fact]
    public void Constructor_ShouldNormalizeLicensePlateAndSectorCode()
    {
        var session = new ParkingSession(
            " abc1234 ",
            " a ",
            CreateUtcDate(12, 0, 0),
            10m);

        Assert.Equal("ABC1234", session.LicensePlate);
        Assert.Equal("A", session.SectorCode);
        Assert.Equal(ParkingSessionStatus.Active, session.Status);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenLicensePlateIsEmpty()
    {
        Action act = () => new ParkingSession(" ", "A", CreateUtcDate(12, 0, 0), 10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("License plate is required.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenSectorCodeIsEmpty()
    {
        Action act = () => new ParkingSession("ABC1234", " ", CreateUtcDate(12, 0, 0), 10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Sector code is required.", exception.Message);
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
    public void AssignParkingSpot_ShouldAssignSpot_WhenSessionIsActiveAndSectorMatches()
    {
        var session = CreateSession();

        session.AssignParkingSpot(10, "A");

        Assert.True(session.HasAssignedSpot);
        Assert.Equal(10, session.ParkingSpotId);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenParkingSpotIdIsInvalid()
    {
        var session = CreateSession();

        Action act = () => session.AssignParkingSpot(0, "A");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking spot id must be greater than zero.", exception.Message);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSpotSectorCodeIsEmpty()
    {
        var session = CreateSession();

        Action act = () => session.AssignParkingSpot(1, " ");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking spot sector code is required.", exception.Message);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSpotSectorDoesNotMatchSessionSector()
    {
        var session = CreateSession();

        Action act = () => session.AssignParkingSpot(1, "B");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking spot sector does not match session sector.", exception.Message);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSpotWasAlreadyAssigned()
    {
        var session = CreateSession();
        session.AssignParkingSpot(1, "A");

        Action act = () => session.AssignParkingSpot(2, "A");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking session already has an assigned spot.", exception.Message);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSessionIsClosed()
    {
        var session = CreateSession();
        session.Close(CreateUtcDate(13, 0, 0), 10m);

        Action act = () => session.AssignParkingSpot(1, "A");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Cannot assign a parking spot to a closed session.", exception.Message);
    }

    [Fact]
    public void Close_ShouldCloseSession_WhenExitTimeAndAmountAreValid()
    {
        var session = CreateSession();
        var exitTimeUtc = CreateUtcDate(13, 0, 0);

        session.Close(exitTimeUtc, 20m);

        Assert.True(session.IsClosed);
        Assert.False(session.IsActive);
        Assert.Equal(exitTimeUtc, session.ExitTimeUtc);
        Assert.Equal(20m, session.ChargedAmount);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenExitTimeIsEarlierThanEntryTime()
    {
        var session = CreateSession();

        Action act = () => session.Close(CreateUtcDate(11, 59, 59), 10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Exit time cannot be earlier than entry time.", exception.Message);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenExitTimeIsNotUtc()
    {
        var session = CreateSession();

        Action act = () => session.Close(
            new DateTime(2025, 1, 1, 13, 0, 0, DateTimeKind.Local),
            10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Exit time must be informed in UTC.", exception.Message);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenChargedAmountIsNegative()
    {
        var session = CreateSession();

        Action act = () => session.Close(CreateUtcDate(13, 0, 0), -1m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Charged amount cannot be negative.", exception.Message);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenSessionIsAlreadyClosed()
    {
        var session = CreateSession();
        session.Close(CreateUtcDate(13, 0, 0), 20m);

        Action act = () => session.Close(CreateUtcDate(14, 0, 0), 30m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking session is already closed.", exception.Message);
    }

    private static ParkingSession CreateSession()
    {
        return new ParkingSession("ABC1234", "A", CreateUtcDate(12, 0, 0), 10m);
    }

    private static DateTime CreateUtcDate(int hour, int minute, int second)
    {
        return new DateTime(2025, 1, 1, hour, minute, second, DateTimeKind.Utc);
    }
}