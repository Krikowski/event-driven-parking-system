using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Estapar.Parking.Infrastructure.Persistence.Converters;

public sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter()
        : base(
            value => value,
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value)
    {
    }
}