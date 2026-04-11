using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.UnitTests.Domain.Entities;

public class ParkingSpotTests {
    [Fact]
    public void Occupy_ShouldThrowDomainException_WhenSpotIsAlreadyOccupied() {
        var spot = new ParkingSpot(1, "A", -23.561684m, -46.655981m);

        spot.Occupy();

        Action act = () => spot.Occupy();

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking spot is already occupied.", exception.Message);
    }

    [Fact]
    public void Release_ShouldMakeSpotAvailable_WhenSpotIsOccupied() {
        var spot = new ParkingSpot(1, "A", -23.561684m, -46.655981m);

        spot.Occupy();
        spot.Release();

        Assert.False(spot.IsOccupied);
    }

    [Fact]
    public void Release_ShouldThrowDomainException_WhenSpotIsAlreadyAvailable() {
        var spot = new ParkingSpot(1, "A", -23.561684m, -46.655981m);

        Action act = () => spot.Release();

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Parking spot is already available.", exception.Message);
    }
}