using System.Text.Json;
using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;
using Estapar.Parking.Domain.Policies;

namespace Estapar.Parking.Application.UseCases.Exit;

public sealed class HandleExitEventUseCase : IHandleExitEventUseCase
{
    private readonly IParkingSessionRepository _parkingSessionRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly IVehicleEventRepository _vehicleEventRepository;
    private readonly IPricingPolicy _pricingPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public HandleExitEventUseCase(
        IParkingSessionRepository parkingSessionRepository,
        ISectorRepository sectorRepository,
        IParkingSpotRepository parkingSpotRepository,
        IVehicleEventRepository vehicleEventRepository,
        IPricingPolicy pricingPolicy,
        IUnitOfWork unitOfWork)
    {
        _parkingSessionRepository = parkingSessionRepository;
        _sectorRepository = sectorRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _vehicleEventRepository = vehicleEventRepository;
        _pricingPolicy = pricingPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        HandleExitEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedLicensePlate = NormalizeLicensePlate(command.LicensePlate);

        var activeSession = await _parkingSessionRepository.GetActiveByLicensePlateAsync(
            normalizedLicensePlate,
            cancellationToken);

        if (activeSession is null)
        {
            throw new DomainException("No active parking session was found for this license plate.");
        }

        var sector = await _sectorRepository.GetByCodeAsync(activeSession.SectorCode, cancellationToken);

        if (sector is null)
        {
            throw new DomainException("Allocated sector was not found for the active parking session.");
        }

        var chargedAmount = _pricingPolicy.CalculateChargedAmount(
            activeSession.EntryTimeUtc,
            command.ExitTimeUtc,
            activeSession.FrozenHourlyRate);

        activeSession.Close(command.ExitTimeUtc, chargedAmount);
        sector.ReleaseCapacity();

        ParkingSpot? assignedParkingSpot = null;

        if (activeSession.ParkingSpotId.HasValue)
        {
            assignedParkingSpot = await _parkingSpotRepository.GetByIdAsync(
                activeSession.ParkingSpotId.Value,
                cancellationToken);

            if (assignedParkingSpot is null)
            {
                throw new DomainException("Assigned parking spot was not found for the active parking session.");
            }

            assignedParkingSpot.Release();
        }

        var vehicleEvent = new VehicleEvent(
            ParkingEventType.Exit,
            normalizedLicensePlate,
            CreatePayloadSnapshot(command, activeSession.SectorCode, chargedAmount, assignedParkingSpot?.Id),
            DateTime.UtcNow);

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
        HandleExitEventCommand command,
        string sectorCode,
        decimal chargedAmount,
        int? parkingSpotId)
    {
        return JsonSerializer.Serialize(new {
            event_type = "EXIT",
            license_plate = command.LicensePlate,
            exit_time = command.ExitTimeUtc,
            sector = sectorCode,
            charged_amount = chargedAmount,
            spot_id = parkingSpotId
        });
    }
}