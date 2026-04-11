using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.UnitTests.Domain.Entities;

public class SectorTests
{
    [Fact]
    public void ConsumeCapacity_ShouldThrowDomainException_WhenSectorIsFull()
    {
        var sector = new Sector("A", 1, 10m);

        sector.ConsumeCapacity();

        Action act = () => sector.ConsumeCapacity();

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Sector capacity has been reached.", exception.Message);
    }

    [Fact]
    public void ReleaseCapacity_ShouldDecreaseAllocatedCapacity_WhenCapacityWasPreviouslyConsumed()
    {
        var sector = new Sector("A", 2, 10m);

        sector.ConsumeCapacity();
        sector.ReleaseCapacity();

        Assert.Equal(0, sector.AllocatedCapacity);
    }

    [Fact]
    public void ReleaseCapacity_ShouldThrowDomainException_WhenThereIsNoAllocatedCapacity()
    {
        var sector = new Sector("A", 2, 10m);

        Action act = () => sector.ReleaseCapacity();

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Sector has no allocated capacity to release.", exception.Message);
    }

    [Fact]
    public void CalculateOccupancyPercentage_ShouldReturnZero_WhenThereIsNoAllocatedCapacity()
    {
        var sector = new Sector("A", 4, 10m);

        var occupancyPercentage = sector.CalculateOccupancyPercentage();

        Assert.Equal(0m, occupancyPercentage);
    }

    [Fact]
    public void CalculateOccupancyPercentage_ShouldReturnExpectedPercentage_WhenCapacityWasConsumed()
    {
        var sector = new Sector("A", 4, 10m);
        sector.ConsumeCapacity();
        sector.ConsumeCapacity();

        var occupancyPercentage = sector.CalculateOccupancyPercentage();

        Assert.Equal(50m, occupancyPercentage);
    }
}
