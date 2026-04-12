using Estapar.Parking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.Infrastructure.Persistence;

public sealed class ParkingDbContext : DbContext
{
    public ParkingDbContext(DbContextOptions<ParkingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
    public DbSet<VehicleEvent> VehicleEvents => Set<VehicleEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParkingDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}