using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Domain.Policies;

public class PricingPolicy : IPricingPolicy
{
    private const decimal FreeMinutesThreshold = 30m;
    private const decimal DiscountMultiplier = 0.90m;
    private const decimal NormalMultiplier = 1.00m;
    private const decimal TenPercentIncreaseMultiplier = 1.10m;
    private const decimal TwentyFivePercentIncreaseMultiplier = 1.25m;

    public decimal CalculateOccupancyMultiplier(decimal occupancyPercentage)
    {
        if (occupancyPercentage < 0 || occupancyPercentage > 100)
        {
            throw new DomainException("Occupancy percentage must be between 0 and 100.");
        }

        if (occupancyPercentage < 25)
        {
            return DiscountMultiplier;
        }

        if (occupancyPercentage <= 50)
        {
            return NormalMultiplier;
        }

        if (occupancyPercentage <= 75)
        {
            return TenPercentIncreaseMultiplier;
        }

        return TwentyFivePercentIncreaseMultiplier;
    }

    public decimal CalculateChargedAmount(DateTime entryTimeUtc, DateTime exitTimeUtc, decimal frozenHourlyRate)
    {
        ValidateTimeRange(entryTimeUtc, exitTimeUtc);

        if (frozenHourlyRate < 0)
        {
            throw new DomainException("Frozen hourly rate cannot be negative.");
        }

        var parkedDuration = exitTimeUtc - entryTimeUtc;
        var totalMinutes = (decimal)parkedDuration.TotalMinutes;

        if (totalMinutes <= FreeMinutesThreshold)
        {
            return 0m;
        }

        var chargedHours = Math.Ceiling(parkedDuration.TotalHours);

        return frozenHourlyRate * (decimal)chargedHours;
    }

    public void ValidateTimeRange(DateTime entryTimeUtc, DateTime exitTimeUtc)
    {
        EnsureUtc(entryTimeUtc, "Entry time");
        EnsureUtc(exitTimeUtc, "Exit time");

        if (exitTimeUtc < entryTimeUtc)
        {
            throw new DomainException("Exit time cannot be earlier than entry time.");
        }
    }

    private static void EnsureUtc(DateTime value, string fieldName)
    {
        if (value == default)
        {
            throw new DomainException($"{fieldName} is required.");
        }

        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"{fieldName} must be informed in UTC.");
        }
    }
}
