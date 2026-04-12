using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Estapar.Parking.Infrastructure.Persistence;

public sealed class ParkingDbContextFactory : IDesignTimeDbContextFactory<ParkingDbContext>
{
    public ParkingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ESTAPAR_PARKING_DATABASE") ??
            "Server=(localdb)\\MSSQLLocalDB;Database=EstaparParkingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<ParkingDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ParkingDbContext(optionsBuilder.Options);
    }
}