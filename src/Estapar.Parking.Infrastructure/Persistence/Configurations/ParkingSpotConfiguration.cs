using Estapar.Parking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estapar.Parking.Infrastructure.Persistence.Configurations;

public sealed class ParkingSpotConfiguration : IEntityTypeConfiguration<ParkingSpot>
{
    public void Configure(EntityTypeBuilder<ParkingSpot> builder)
    {
        builder.ToTable("ParkingSpots");

        builder.HasKey(spot => spot.Id);

        builder.Property(spot => spot.Id)
            .ValueGeneratedNever();

        builder.Property(spot => spot.SectorCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(spot => spot.Latitude)
            .HasPrecision(9, 6)
            .IsRequired();

        builder.Property(spot => spot.Longitude)
            .HasPrecision(9, 6)
            .IsRequired();

        builder.Property(spot => spot.IsOccupied)
            .IsRequired();

        builder.HasIndex(spot => new { spot.SectorCode, spot.Latitude, spot.Longitude })
            .IsUnique();

        builder.HasOne<Sector>()
            .WithMany()
            .HasForeignKey(spot => spot.SectorCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}