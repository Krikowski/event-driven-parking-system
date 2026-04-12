using System.Text.Json;
using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Application.UseCases.Parked;

public sealed class HandleParkedEventUseCase : IHandleParkedEventUseCase
{
    private readonly IParkingSessionRepository _parkingSessionRepository;
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly IVehicleEventRepository _vehicleEventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public HandleParkedEventUseCase(
        IParkingSessionRepository parkingSessionRepository,
        IParkingSpotRepository parkingSpotRepository,
        IVehicleEventRepository vehicleEventRepository,
        IUnitOfWork unitOfWork)
    {
        _parkingSessionRepository = parkingSessionRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _vehicleEventRepository = vehicleEventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        HandleParkedEventCommand command,
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

        if (activeSession.ParkingSpotId is not null)
        {
            throw new DomainException("Parking session is already associated with a parking spot.");
        }

        var parkingSpot = await _parkingSpotRepository.GetByCoordinatesAsync(
            command.Latitude,
            command.Longitude,
            cancellationToken);

        if (parkingSpot is null)
        {
            throw new DomainException("Parking spot was not found for the provided coordinates.");
        }

        if (parkingSpot.IsOccupied)
        {
            throw new DomainException("Parking spot is already occupied.");
        }

        activeSession.AssignParkingSpot(parkingSpot.Id, parkingSpot.SectorCode);
        parkingSpot.Occupy();

        var vehicleEvent = new VehicleEvent(
            ParkingEventType.Parked,
            normalizedLicensePlate,
            CreatePayloadSnapshot(command, parkingSpot.Id, activeSession.SectorCode),
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
        HandleParkedEventCommand command,
        int parkingSpotId,
        string sectorCode)
    {
        return JsonSerializer.Serialize(new {
            event_type = "PARKED",
            license_plate = command.LicensePlate,
            lat = command.Latitude,
            lng = command.Longitude,
            spot_id = parkingSpotId,
            sector = sectorCode
        });
    }
}