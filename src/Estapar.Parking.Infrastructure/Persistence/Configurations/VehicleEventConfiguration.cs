using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estapar.Parking.Infrastructure.Persistence.Configurations;

public sealed class VehicleEventConfiguration : IEntityTypeConfiguration<VehicleEvent>
{
    public void Configure(EntityTypeBuilder<VehicleEvent> builder)
    {
        builder.ToTable("VehicleEvents");

        builder.Property<int>("Id")
            .ValueGeneratedOnAdd();

        builder.HasKey("Id");

        builder.Property(vehicleEvent => vehicleEvent.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(vehicleEvent => vehicleEvent.LicensePlate)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(vehicleEvent => vehicleEvent.PayloadSnapshot)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(vehicleEvent => vehicleEvent.ProcessedAtUtc)
            .HasConversion<UtcDateTimeConverter>()
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(vehicleEvent => vehicleEvent.LicensePlate);
        builder.HasIndex(vehicleEvent => new { vehicleEvent.EventType, vehicleEvent.ProcessedAtUtc });
    }
}