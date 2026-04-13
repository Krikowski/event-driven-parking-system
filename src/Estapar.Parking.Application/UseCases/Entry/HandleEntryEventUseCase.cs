using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Common.Webhooks;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;
using Estapar.Parking.Domain.Policies;

using Microsoft.Extensions.Logging;

namespace Estapar.Parking.Application.UseCases.Entry;

public sealed class HandleEntryEventUseCase : WebhookUseCaseBase, IHandleEntryEventUseCase
{
    private readonly IParkingSessionRepository _parkingSessionRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IPricingPolicy _pricingPolicy;
    private readonly ILogger<HandleEntryEventUseCase> _logger;

    public HandleEntryEventUseCase(
        IParkingSessionRepository parkingSessionRepository,
        ISectorRepository sectorRepository,
        IVehicleEventRepository vehicleEventRepository,
        IPricingPolicy pricingPolicy,
        IUnitOfWork unitOfWork,
        ILogger<HandleEntryEventUseCase> logger)
        : base(vehicleEventRepository, unitOfWork)
    {
        _parkingSessionRepository = parkingSessionRepository;
        _sectorRepository = sectorRepository;
        _pricingPolicy = pricingPolicy;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        HandleEntryEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedLicensePlate = NormalizeLicensePlate(command.LicensePlate);
        var idempotencyKey = VehicleEventIdempotencyKeyFactory.CreateForEntry(
            normalizedLicensePlate,
            command.EntryTimeUtc);

        _logger.LogInformation(
            "Processing webhook event {EventType} for license plate {LicensePlate}.",
            "ENTRY",
            normalizedLicensePlate);

        if (await HasAlreadyBeenProcessedAsync(idempotencyKey, cancellationToken))
        {
            _logger.LogInformation(
                "Ignoring duplicate webhook event {EventType} for license plate {LicensePlate}.",
                "ENTRY",
                normalizedLicensePlate);

            return;
        }

        var hasActiveSession = await _parkingSessionRepository.ExistsActiveByLicensePlateAsync(
            normalizedLicensePlate,
            cancellationToken);

        if (hasActiveSession)
        {
            throw new DomainException("An active parking session already exists for this license plate.");
        }

        var sectors = await _sectorRepository.GetAllAsync(cancellationToken);
        var selectedSector = SelectSectorForEntry(sectors);

        if (selectedSector is null)
        {
            throw new DomainException("Parking lot is full.");
        }

        var occupancyPercentageAtEntry = selectedSector.CalculateOccupancyPercentage();
        var occupancyMultiplier = _pricingPolicy.CalculateOccupancyMultiplier(occupancyPercentageAtEntry);
        var frozenHourlyRate = selectedSector.BasePrice * occupancyMultiplier;

        selectedSector.ConsumeCapacity();

        var parkingSession = new ParkingSession(
            normalizedLicensePlate,
            selectedSector.Code,
            command.EntryTimeUtc,
            frozenHourlyRate);

        var vehicleEvent = VehicleEventFactory.Create(
            idempotencyKey,
            ParkingEventType.Entry,
            normalizedLicensePlate,
            new
            {
                event_type = "ENTRY",
                license_plate = command.LicensePlate,
                entry_time = command.EntryTimeUtc,
                sector = selectedSector.Code,
                frozen_hourly_rate = frozenHourlyRate
            });

        await _parkingSessionRepository.AddAsync(parkingSession, cancellationToken);
        await AddVehicleEventAsync(vehicleEvent, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Webhook event {EventType} processed successfully for license plate {LicensePlate} in sector {Sector}.",
            "ENTRY",
            normalizedLicensePlate,
            selectedSector.Code);
    }

    private static Sector? SelectSectorForEntry(IEnumerable<Sector> sectors)
    {
        return sectors
            .Where(sector => sector.HasAvailableCapacity)
            .OrderBy(sector => sector.CalculateOccupancyPercentage())
            .ThenBy(sector => sector.BasePrice)
            .ThenBy(sector => sector.Code, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}