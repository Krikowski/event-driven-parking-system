using System.Text.Json;
using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;
using Estapar.Parking.Domain.Policies;

namespace Estapar.Parking.Application.UseCases.Entry;

public sealed class HandleEntryEventUseCase : IHandleEntryEventUseCase
{
    private readonly IParkingSessionRepository _parkingSessionRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IVehicleEventRepository _vehicleEventRepository;
    private readonly IPricingPolicy _pricingPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public HandleEntryEventUseCase(
        IParkingSessionRepository parkingSessionRepository,
        ISectorRepository sectorRepository,
        IVehicleEventRepository vehicleEventRepository,
        IPricingPolicy pricingPolicy,
        IUnitOfWork unitOfWork)
    {
        _parkingSessionRepository = parkingSessionRepository;
        _sectorRepository = sectorRepository;
        _vehicleEventRepository = vehicleEventRepository;
        _pricingPolicy = pricingPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        HandleEntryEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedLicensePlate = NormalizeLicensePlate(command.LicensePlate);

        var hasActiveSession = await _parkingSessionRepository.ExistsActiveByLicensePlateAsync(
            normalizedLicensePlate,
            cancellationToken);

        if (hasActiveSession)
        {
            throw new DomainException("An active parking session already exists for this license plate.");
        }

        var sectors = await _sectorRepository.GetAllAsync(cancellationToken);

        var selectedSector = sectors
            .Where(sector => sector.HasAvailableCapacity)
            .OrderBy(sector => sector.Code, StringComparer.Ordinal)
            .FirstOrDefault();

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

        var vehicleEvent = new VehicleEvent(
            ParkingEventType.Entry,
            normalizedLicensePlate,
            CreatePayloadSnapshot(command, selectedSector.Code, frozenHourlyRate),
            DateTime.UtcNow);

        await _parkingSessionRepository.AddAsync(parkingSession, cancellationToken);
        await _vehicleEventRepository.AddAsync(vehicleEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeLicensePlate(string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            throw new DomainException("License plate is required.");
        }

        return licensePlate.Trim().ToUpperInvariant();
    }

    private static string CreatePayloadSnapshot(
        HandleEntryEventCommand command,
        string sectorCode,
        decimal frozenHourlyRate)
    {
        return JsonSerializer.Serialize(new {
            event_type = "ENTRY",
            license_plate = command.LicensePlate,
            entry_time = command.EntryTimeUtc,
            sector = sectorCode,
            frozen_hourly_rate = frozenHourlyRate
        });
    }
}