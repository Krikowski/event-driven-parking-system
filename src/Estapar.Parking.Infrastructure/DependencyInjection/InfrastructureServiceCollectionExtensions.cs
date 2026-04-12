using Estapar.Parking.Application.Abstractions.Integrations;
using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Domain.Policies;
using Estapar.Parking.Infrastructure.Integrations;
using Estapar.Parking.Infrastructure.Persistence;
using Estapar.Parking.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estapar.Parking.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ParkingDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ParkingDatabase' was not found.");
        }

        var garageApiBaseUrl = configuration[$"{GarageApiOptions.SectionName}:BaseUrl"];

        if (string.IsNullOrWhiteSpace(garageApiBaseUrl))
        {
            throw new InvalidOperationException("Garage API base URL was not configured.");
        }

        services.AddDbContext<ParkingDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddHttpClient<IGarageConfigurationClient, GarageConfigurationClient>(client => {
            client.BaseAddress = new Uri(garageApiBaseUrl);
        });

        services.AddScoped<ISectorRepository, SectorRepository>();
        services.AddScoped<IParkingSpotRepository, ParkingSpotRepository>();
        services.AddScoped<IParkingSessionRepository, ParkingSessionRepository>();
        services.AddScoped<IVehicleEventRepository, VehicleEventRepository>();
        services.AddScoped<IRevenueReadRepository, RevenueReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPricingPolicy, PricingPolicy>();

        return services;
    }
}