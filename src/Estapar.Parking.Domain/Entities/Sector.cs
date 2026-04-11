using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Domain.Entities;

public class Sector {
    public string Code { get; }
    public int MaxCapacity { get; }
    public decimal BasePrice { get; }
    public int AllocatedCapacity { get; private set; }

    public bool IsFull => AllocatedCapacity >= MaxCapacity;

    public Sector(string code, int maxCapacity, decimal basePrice) {
        if (string.IsNullOrWhiteSpace(code)) {
            throw new DomainException("Sector code is required.");
        }

        if (maxCapacity <= 0) {
            throw new DomainException("Sector max capacity must be greater than zero.");
        }

        if (basePrice < 0) {
            throw new DomainException("Sector base price cannot be negative.");
        }

        Code = code.Trim().ToUpperInvariant();
        MaxCapacity = maxCapacity;
        BasePrice = basePrice;
        AllocatedCapacity = 0;
    }

    public void ConsumeCapacity() {
        if (IsFull) {
            throw new DomainException("Sector capacity has been reached.");
        }

        AllocatedCapacity++;
    }

    public void ReleaseCapacity() {
        if (AllocatedCapacity <= 0) {
            throw new DomainException("Sector has no occupied spaces to release.");
        }

        AllocatedCapacity--;
    }

    public decimal CalculateOccupancyPercentage() {
        return (decimal)AllocatedCapacity / MaxCapacity * 100m;
    }
}