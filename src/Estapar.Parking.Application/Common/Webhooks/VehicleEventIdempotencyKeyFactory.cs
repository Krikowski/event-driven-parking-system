using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Estapar.Parking.Domain.Enums;

namespace Estapar.Parking.Application.Common.Webhooks;

public static class VehicleEventIdempotencyKeyFactory
{
    public static string CreateForEntry(
        string normalizedLicensePlate,
        DateTime entryTimeUtc)
    {
        EnsureUtc(entryTimeUtc, nameof(entryTimeUtc));

        return CreateHash(
            ParkingEventType.Entry,
            normalizedLicensePlate,
            entryTimeUtc.ToString("O", CultureInfo.InvariantCulture));
    }

    public static string CreateForParked(
        string normalizedLicensePlate,
        DateTime sessionEntryTimeUtc,
        decimal latitude,
        decimal longitude)
    {
        EnsureUtc(sessionEntryTimeUtc, nameof(sessionEntryTimeUtc));

        return CreateHash(
            ParkingEventType.Parked,
            normalizedLicensePlate,
            sessionEntryTimeUtc.ToString("O", CultureInfo.InvariantCulture),
            latitude.ToString(CultureInfo.InvariantCulture),
            longitude.ToString(CultureInfo.InvariantCulture));
    }

    public static string CreateForExit(
        string normalizedLicensePlate,
        DateTime exitTimeUtc)
    {
        EnsureUtc(exitTimeUtc, nameof(exitTimeUtc));

        return CreateHash(
            ParkingEventType.Exit,
            normalizedLicensePlate,
            exitTimeUtc.ToString("O", CultureInfo.InvariantCulture));
    }

    private static string CreateHash(
        ParkingEventType eventType,
        string normalizedLicensePlate,
        params string[] components)
    {
        var rawKey = string.Join(
            '|',
            new[] { eventType.ToString().ToUpperInvariant(), normalizedLicensePlate }
                .Concat(components));

        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    private static void EnsureUtc(DateTime value, string paramName)
    {
        if (value == default)
        {
            throw new ArgumentException("Timestamp is required.", paramName);
        }

        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be informed in UTC.", paramName);
        }
    }
}