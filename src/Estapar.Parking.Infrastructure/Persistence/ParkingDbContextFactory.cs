using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Estapar.Parking.Infrastructure.Persistence;

public sealed class ParkingDbContextFactory : IDesignTimeDbContextFactory<ParkingDbContext>
{
    public ParkingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ParkingDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=EstaparParkingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new ParkingDbContext(optionsBuilder.Options);
    }
}