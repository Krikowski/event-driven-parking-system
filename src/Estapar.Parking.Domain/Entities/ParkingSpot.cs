using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Domain.Entities;

public class ParkingSpot
{
    public int Id { get; }
    public string SectorCode { get; }
    public decimal Latitude { get; }
    public decimal Longitude { get; }
    public bool IsOccupied { get; private set; }


    public ParkingSpot(int id, string sectorCode, decimal latitude, decimal longitude)
    {
        if (id <= 0)
        {
            throw new DomainException("Parking spot id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(sectorCode))
        {
            throw new DomainException("Parking spot sector code is required.");
        }

        Id = id;
        SectorCode = sectorCode.Trim().ToUpperInvariant();
        Latitude = latitude;
        Longitude = longitude;
        IsOccupied = false;
    }

    public void Occupy()
    {
        if (IsOccupied)
        {
            throw new DomainException("Parking spot is already occupied.");
        }

        IsOccupied = true;
    }

    public void Release()
    {
        if (!IsOccupied)
        {
            throw new DomainException("Parking spot is already available.");
        }

        IsOccupied = false;
    }
}