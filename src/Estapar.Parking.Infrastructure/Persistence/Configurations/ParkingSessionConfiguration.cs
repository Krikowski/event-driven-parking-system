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

        builder.Property(parkingSession => parkingSession.LicensePlate)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(parkingSession => parkingSession.SectorCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(parkingSession => parkingSession.EntryTimeUtc)
            .HasConversion<UtcDateTimeConverter>()
            .IsRequired();

        builder.Property(parkingSession => parkingSession.ExitTimeUtc);

        builder.Property(parkingSession => parkingSession.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(parkingSession => parkingSession.FrozenHourlyRate)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(parkingSession => parkingSession.ChargedAmount)
            .HasPrecision(18, 2);

        builder.HasOne<Sector>()
            .WithMany()
            .HasForeignKey(parkingSession => parkingSession.SectorCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ParkingSpot>()
            .WithMany()
            .HasForeignKey(parkingSession => parkingSession.ParkingSpotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(parkingSession => parkingSession.LicensePlate)
            .HasDatabaseName("IX_ParkingSessions_ActiveLicensePlate")
            .IsUnique()
            .HasFilter("[Status] = 1");

        builder.HasIndex(parkingSession => parkingSession.ParkingSpotId)
            .HasDatabaseName("IX_ParkingSessions_ActiveParkingSpot")
            .IsUnique()
            .HasFilter("[Status] = 1 AND [ParkingSpotId] IS NOT NULL");

        builder.HasIndex(parkingSession => new
        {
            parkingSession.SectorCode,
            parkingSession.ExitTimeUtc
        })
        .HasDatabaseName("IX_ParkingSessions_RevenueBySectorAndExitTime")
        .HasFilter("[ExitTimeUtc] IS NOT NULL");
    }
}