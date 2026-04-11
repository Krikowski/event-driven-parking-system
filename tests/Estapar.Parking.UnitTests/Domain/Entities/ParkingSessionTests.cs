using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.UnitTests.Domain.Entities;

public class ParkingSessionTests
{
    [Fact]
    public void Constructor_ShouldNormalizeLicensePlate()
    {
        var session = new ParkingSession(" abc1234 ", "A", new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), 10m);

        Assert.Equal("ABC1234", session.LicensePlate);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenSessionIsAlreadyClosed()
    {
        var session = new ParkingSession("ABC1234", "A", new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), 10m);

        session.Close(new DateTime(2025, 1, 1, 13, 0, 0, DateTimeKind.Utc), 10m);

        Action act = () => session.Close(new DateTime(2025, 1, 1, 14, 0, 0, DateTimeKind.Utc), 20m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking session is already closed.", exception.Message);
    }

    [Fact]
    public void Close_ShouldThrowDomainException_WhenExitTimeIsEarlierThanEntryTime()
    {
        var session = new ParkingSession("ABC1234", "A", new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), 10m);

        Action act = () => session.Close(new DateTime(2025, 1, 1, 11, 59, 59, DateTimeKind.Utc), 10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Exit time cannot be earlier than entry time.", exception.Message);
    }

    [Fact]
    public void Close_ShouldSetExitTimeChargedAmountAndClosedStatus()
    {
        var exitTimeUtc = new DateTime(2025, 1, 1, 13, 0, 0, DateTimeKind.Utc);
        var session = new ParkingSession("ABC1234", "A", new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), 10m);

        session.Close(exitTimeUtc, 10m);

        Assert.Equal(exitTimeUtc, session.ExitTimeUtc);
        Assert.Equal(10m, session.ChargedAmount);
        Assert.Equal(ParkingSessionStatus.Closed, session.Status);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSessionAlreadyHasAnAssignedSpot()
    {
        var session = new ParkingSession("ABC1234", "A", new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), 10m);

        session.AssignParkingSpot(1, "A");

        Action act = () => session.AssignParkingSpot(2, "A");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking session already has an assigned spot.", exception.Message);
    }

    [Fact]
    public void AssignParkingSpot_ShouldThrowDomainException_WhenSpotSectorDoesNotMatchSessionSector()
    {
        var session = new ParkingSession("ABC1234", "A", new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc), 10m);

        Action act = () => session.AssignParkingSpot(1, "B");

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking spot sector does not match session sector.", exception.Message);
    }
}