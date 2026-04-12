using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Estapar.Parking.Infrastructure.Persistence.Converters;

public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}