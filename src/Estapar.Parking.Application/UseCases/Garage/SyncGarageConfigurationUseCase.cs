using Estapar.Parking.Application.Abstractions.Integrations;
using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Integrations;
using Estapar.Parking.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Estapar.Parking.Application.UseCases.Garage;

public sealed class SyncGarageConfigurationUseCase : ISyncGarageConfigurationUseCase
{
    private readonly IGarageConfigurationClient _garageConfigurationClient;
    private readonly ISectorRepository _sectorRepository;
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncGarageConfigurationUseCase> _logger;

    public SyncGarageConfigurationUseCase(
        IGarageConfigurationClient garageConfigurationClient,
        ISectorRepository sectorRepository,
        IParkingSpotRepository parkingSpotRepository,
        IUnitOfWork unitOfWork,
        ILogger<SyncGarageConfigurationUseCase> logger)
    {
        _garageConfigurationClient = garageConfigurationClient;
        _sectorRepository = sectorRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var hasSectors = await _sectorRepository.AnyAsync(cancellationToken);
        var hasSpots = await _parkingSpotRepository.AnyAsync(cancellationToken);

        if (hasSectors && hasSpots)
        {
            _logger.LogInformation("Garage configuration sync skipped because data already exists.");
            return;
        }

        if (hasSectors != hasSpots)
        {
            throw new InvalidOperationException(
                "Garage configuration is partially persisted. Manual intervention is required.");
        }

        _logger.LogInformation("Synchronizing garage configuration from external source.");

        var configuration = await _garageConfigurationClient.GetConfigurationAsync(cancellationToken);

        ValidateConfiguration(configuration);

        var sectors = configuration.Sectors
            .Select(sectorDto => new Sector(
                sectorDto.Sector,
                sectorDto.MaxCapacity,
                sectorDto.BasePrice))
            .ToList();

        var parkingSpots = configuration.Spots
            .Select(spotDto => new ParkingSpot(
                spotDto.Id,
                spotDto.Sector,
                spotDto.Latitude,
                spotDto.Longitude))
            .ToList();

        EnsureAllParkingSpotsReferenceExistingSectors(parkingSpots, sectors);

        await _sectorRepository.AddRangeAsync(sectors, cancellationToken);
        await _parkingSpotRepository.AddRangeAsync(parkingSpots, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Garage configuration synchronized successfully with {SectorCount} sectors and {SpotCount} spots.",
            sectors.Count,
            parkingSpots.Count);
    }

    private static void ValidateConfiguration(GarageConfigurationDto configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.Sectors.Count == 0)
        {
            throw new InvalidOperationException("Garage configuration does not contain sectors.");
        }

        if (configuration.Spots.Count == 0)
        {
            throw new InvalidOperationException("Garage configuration does not contain parking spots.");
        }
    }

    private static void EnsureAllParkingSpotsReferenceExistingSectors(
        IEnumerable<ParkingSpot> parkingSpots,
        IEnumerable<Sector> sectors)
    {
        var sectorCodes = sectors
            .Select(sector => sector.Code)
            .ToHashSet(StringComparer.Ordinal);

        var invalidParkingSpot = parkingSpots
            .FirstOrDefault(parkingSpot => !sectorCodes.Contains(parkingSpot.SectorCode));

        if (invalidParkingSpot is not null)
        {
            throw new InvalidOperationException(
                $"Parking spot '{invalidParkingSpot.Id}' references unknown sector '{invalidParkingSpot.SectorCode}'.");
        }
    }
}