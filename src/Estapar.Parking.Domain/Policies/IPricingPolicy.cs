namespace Estapar.Parking.Domain.Policies;

public interface IPricingPolicy
{
    /// <summary>
    /// Calculates the pricing multiplier based on the occupancy percentage at the moment of entry.
    /// </summary>
    decimal CalculateOccupancyMultiplier(decimal occupancyPercentage);

    /// <summary>
    /// Calculates the charged amount based on the frozen hourly rate and parking duration.
    /// </summary>
    decimal CalculateChargedAmount(DateTime entryTimeUtc, DateTime exitTimeUtc, decimal frozenHourlyRate);

    /// <summary>
    /// Validates whether the exit time is not earlier than the entry time.
    /// </summary>
    void ValidateTimeRange(DateTime entryTimeUtc, DateTime exitTimeUtc);
}