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
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new DomainException("License plate is required.");
        }

        if (string.IsNullOrWhiteSpace(sectorCode))
        {
            throw new DomainException("Sector code is required.");
        }

        if (entryTimeUtc == default)
        {
            throw new DomainException("Entry time is required.");
        }

        if (frozenHourlyRate < 0)
        {
            throw new DomainException("Frozen hourly rate cannot be negative.");
        }

        LicensePlate = NormalizeLicensePlate(licensePlate);
        SectorCode = sectorCode.Trim().ToUpperInvariant();
        EntryTimeUtc = entryTimeUtc;
        FrozenHourlyRate = frozenHourlyRate;
        Status = ParkingSessionStatus.Active;
    }

    public void AssignParkingSpot(int parkingSpotId, string spotSectorCode)
    {
        if (!IsActive)
        {
            throw new DomainException("Cannot assign a parking spot to a closed session.");
        }

        if (HasAssignedSpot)
        {
            throw new DomainException("Parking session already has an assigned spot.");
        }

        if (parkingSpotId <= 0)
        {
            throw new DomainException("Parking spot id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(spotSectorCode))
        {
            throw new DomainException("Parking spot sector code is required.");
        }

        if (!SectorCode.Equals(spotSectorCode.Trim().ToUpperInvariant(), StringComparison.Ordinal))
        {
            throw new DomainException("Parking spot sector does not match session sector.");
        }

        ParkingSpotId = parkingSpotId;
    }

    public void Close(DateTime exitTimeUtc, decimal chargedAmount)
    {
        if (IsClosed)
        {
            throw new DomainException("Parking session is already closed.");
        }

        if (exitTimeUtc < EntryTimeUtc)
        {
            throw new DomainException("Exit time cannot be earlier than entry time.");
        }

        if (chargedAmount < 0)
        {
            throw new DomainException("Charged amount cannot be negative.");
        }

        ExitTimeUtc = exitTimeUtc;
        ChargedAmount = chargedAmount;
        Status = ParkingSessionStatus.Closed;
    }

    private static string NormalizeLicensePlate(string licensePlate)
    {
        return licensePlate.Trim().ToUpperInvariant();
    }
}