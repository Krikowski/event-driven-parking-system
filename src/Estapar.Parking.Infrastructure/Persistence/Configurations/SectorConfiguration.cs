using Estapar.Parking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estapar.Parking.Infrastructure.Persistence.Configurations;

public sealed class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        builder.ToTable("Sectors");

        builder.HasKey(sector => sector.Code);

        builder.Property(sector => sector.Code)
            .HasMaxLength(16)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(sector => sector.MaxCapacity)
            .IsRequired();

        builder.Property(sector => sector.BasePrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(sector => sector.AllocatedCapacity)
            .IsRequired();
    }
}