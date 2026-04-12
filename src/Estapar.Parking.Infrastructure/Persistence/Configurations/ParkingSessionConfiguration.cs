using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estapar.Parking.Infrastructure.Persistence.Configurations;

public sealed class ParkingSessionConfiguration : IEntityTypeConfiguration<ParkingSession>
{
    public void Configure(EntityTypeBuilder<ParkingSession> builder)
    {
        builder.ToTable("ParkingSessions");

        builder.Property<int>("Id")
            .ValueGeneratedOnAdd();

        builder.HasKey("Id");

        builder.Property(session => session.LicensePlate)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(session => session.SectorCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(session => session.ParkingSpotId)
            .IsRequired(false);

        builder.Property(session => session.EntryTimeUtc)
            .HasConversion<UtcDateTimeConverter>()
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(session => session.ExitTimeUtc)
            .HasConversion<UtcNullableDateTimeConverter>()
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(session => session.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(session => session.FrozenHourlyRate)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(session => session.ChargedAmount)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.HasIndex(session => session.LicensePlate);

        builder.HasIndex(session => session.LicensePlate)
            .HasDatabaseName("IX_ParkingSessions_ActiveLicensePlate")
            .IsUnique()
            .HasFilter("[Status] = 1");

        builder.HasIndex(session => new { session.LicensePlate, session.Status });

        builder.HasIndex(session => new { session.SectorCode, session.EntryTimeUtc });

        builder.HasOne<Sector>()
            .WithMany()
            .HasForeignKey(session => session.SectorCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ParkingSpot>()
            .WithMany()
            .HasForeignKey(session => session.ParkingSpotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}