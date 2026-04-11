using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Domain.Entities;

public class ParkingSession
{
    public string LicensePlate { get; }
    public string SectorCode { get; }
    public int? ParkingSpotId { get; private set; }
    public DateTime EntryTimeUtc { get; }
    public DateTime? ExitTimeUtc { get; private set; }
    public ParkingSessionStatus Status { get; private set; }
    public decimal FrozenHourlyRate { get; }
    public decimal? ChargedAmount { get; private set; }

    public bool HasAssignedSpot => ParkingSpotId.HasValue;
    public bool IsActive => Status == ParkingSessionStatus.Active;
    public bool IsClosed => Status == ParkingSessionStatus.Closed;

    public ParkingSession(
        string licensePlate,
        string sectorCode,
        DateTime entryTimeUtc,
        decimal frozenHourlyRate)
    {
        LicensePlate = NormalizeLicensePlate(licensePlate);
        SectorCode = NormalizeSectorCode(sectorCode);
        EntryTimeUtc = EnsureUtc(entryTimeUtc, "Entry time");
        FrozenHourlyRate = EnsureNonNegative(frozenHourlyRate, "Frozen hourly rate");
        Status = ParkingSessionStatus.Active;
    }

    public void AssignParkingSpot(int parkingSpotId, string spotSectorCode)
    {
        EnsureSessionIsActiveForSpotAssignment();

        if (HasAssignedSpot)
        {
            throw new DomainException("Parking session already has an assigned spot.");
        }

        if (parkingSpotId <= 0)
        {
            throw new DomainException("Parking spot id must be greater than zero.");
        }

        var normalizedSpotSectorCode = NormalizeSectorCode(spotSectorCode, "Parking spot sector code");

        if (!SectorCode.Equals(normalizedSpotSectorCode, StringComparison.Ordinal))
        {
            throw new DomainException("Parking spot sector does not match session sector.");
        }

        ParkingSpotId = parkingSpotId;
    }

    public void Close(DateTime exitTimeUtc, decimal chargedAmount)
    {
        EnsureSessionIsOpen();

        var validatedExitTimeUtc = EnsureUtc(exitTimeUtc, "Exit time");

        if (validatedExitTimeUtc < EntryTimeUtc)
        {
            throw new DomainException("Exit time cannot be earlier than entry time.");
        }

        ExitTimeUtc = validatedExitTimeUtc;
        ChargedAmount = EnsureNonNegative(chargedAmount, "Charged amount");
        Status = ParkingSessionStatus.Closed;
    }

    private void EnsureSessionIsOpen()
    {
        if (IsClosed)
        {
            throw new DomainException("Parking session is already closed.");
        }
    }

    private void EnsureSessionIsActiveForSpotAssignment()
    {
        if (!IsActive)
        {
            throw new DomainException("Cannot assign a parking spot to a closed session.");
        }
    }

    private static string NormalizeLicensePlate(string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new DomainException("License plate is required.");
        }

        return licensePlate.Trim().ToUpperInvariant();
    }

    private static string NormalizeSectorCode(string sectorCode, string fieldName = "Sector code")
    {
        if (string.IsNullOrWhiteSpace(sectorCode))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return sectorCode.Trim().ToUpperInvariant();
    }

    private static DateTime EnsureUtc(DateTime value, string fieldName)
    {
        if (value == default)
        {
            throw new DomainException($"{fieldName} is required.");
        }

        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"{fieldName} must be informed in UTC.");
        }

        return value;
    }

    private static decimal EnsureNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        return value;
    }
}
