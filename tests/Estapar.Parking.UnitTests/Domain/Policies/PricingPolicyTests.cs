using Estapar.Parking.Domain.Exceptions;
using Estapar.Parking.Domain.Policies;

namespace Estapar.Parking.UnitTests.Domain.Policies;

public class PricingPolicyTests
{
    private readonly PricingPolicy _pricingPolicy = new();

    [Theory]
    [InlineData(0.00, 0.90)]
    [InlineData(24.99, 0.90)]
    [InlineData(25.00, 1.00)]
    [InlineData(50.00, 1.00)]
    [InlineData(75.00, 1.10)]
    [InlineData(100.00, 1.25)]
    public void CalculateOccupancyMultiplier_ShouldReturnExpectedMultiplier_WhenOccupancyIsAtBoundary(
        decimal occupancyPercentage,
        decimal expectedMultiplier)
    {
        var multiplier = _pricingPolicy.CalculateOccupancyMultiplier(occupancyPercentage);

        Assert.Equal(expectedMultiplier, multiplier);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void CalculateOccupancyMultiplier_ShouldThrowDomainException_WhenOccupancyIsOutOfRange(
        decimal occupancyPercentage)
    {
        Action act = () => _pricingPolicy.CalculateOccupancyMultiplier(occupancyPercentage);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Occupancy percentage must be between 0 and 100.", exception.Message);
    }

    [Theory]
    [InlineData(0, 29, 59, 0)]
    [InlineData(0, 30, 0, 0)]
    [InlineData(0, 30, 1, 10)]
    [InlineData(1, 0, 1, 20)]
    public void CalculateChargedAmount_ShouldReturnExpectedAmount_WhenParkingDurationIsAtBoundary(
        int hours,
        int minutes,
        int seconds,
        decimal expectedAmount)
    {
        const decimal frozenHourlyRate = 10m;
        var entryTimeUtc = CreateUtcDate(12, 0, 0);
        var exitTimeUtc = entryTimeUtc
            .AddHours(hours)
            .AddMinutes(minutes)
            .AddSeconds(seconds);

        var chargedAmount = _pricingPolicy.CalculateChargedAmount(entryTimeUtc, exitTimeUtc, frozenHourlyRate);

        Assert.Equal(expectedAmount, chargedAmount);
    }

    [Fact]
    public void CalculateChargedAmount_ShouldThrowDomainException_WhenExitTimeIsEarlierThanEntryTime()
    {
        var entryTimeUtc = CreateUtcDate(12, 0, 0);
        var exitTimeUtc = entryTimeUtc.AddSeconds(-1);

        Action act = () => _pricingPolicy.CalculateChargedAmount(entryTimeUtc, exitTimeUtc, 10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Exit time cannot be earlier than entry time.", exception.Message);
    }

    [Fact]
    public void CalculateChargedAmount_ShouldThrowDomainException_WhenEntryTimeIsNotUtc()
    {
        var exitTimeUtc = CreateUtcDate(13, 0, 0);
        var localEntryTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local);

        Action act = () => _pricingPolicy.CalculateChargedAmount(localEntryTime, exitTimeUtc, 10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Entry time must be informed in UTC.", exception.Message);
    }

    [Fact]
    public void CalculateChargedAmount_ShouldThrowDomainException_WhenExitTimeIsNotUtc()
    {
        var entryTimeUtc = CreateUtcDate(12, 0, 0);
        var localExitTime = new DateTime(2025, 1, 1, 13, 0, 0, DateTimeKind.Local);

        Action act = () => _pricingPolicy.CalculateChargedAmount(entryTimeUtc, localExitTime, 10m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Exit time must be informed in UTC.", exception.Message);
    }

    [Fact]
    public void CalculateChargedAmount_ShouldThrowDomainException_WhenFrozenHourlyRateIsNegative()
    {
        var entryTimeUtc = CreateUtcDate(12, 0, 0);
        var exitTimeUtc = CreateUtcDate(13, 0, 0);

        Action act = () => _pricingPolicy.CalculateChargedAmount(entryTimeUtc, exitTimeUtc, -1m);

        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Frozen hourly rate cannot be negative.", exception.Message);
    }

    private static DateTime CreateUtcDate(int hour, int minute, int second)
    {
        return new DateTime(2025, 1, 1, hour, minute, second, DateTimeKind.Utc);
    }
}