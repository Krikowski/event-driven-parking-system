using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Application.Common.Webhooks;

public static class LicensePlateNormalizer
{
    public static string Normalize(string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new DomainException("License plate is required.");
        }

        return licensePlate.Trim().ToUpperInvariant();
    }
}