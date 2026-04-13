using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Common.Webhooks;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Application.UseCases.Parked;

public sealed class HandleParkedEventUseCase : WebhookUseCaseBase, IHandleParkedEventUseCase
{
    private readonly IParkingSessionRepository _parkingSessionRepository;
    private readonly IParkingSpotRepository _parkingSpotRepository;

    public HandleParkedEventUseCase(
        IParkingSessionRepository parkingSessionRepository,
        IParkingSpotRepository parkingSpotRepository,
        IVehicleEventRepository vehicleEventRepository,
        IUnitOfWork unitOfWork)
        : base(vehicleEventRepository, unitOfWork)
    {
        _parkingSessionRepository = parkingSessionRepository;
        _parkingSpotRepository = parkingSpotRepository;
    }

    public async Task ExecuteAsync(
        HandleParkedEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedLicensePlate = NormalizeLicensePlate(command.LicensePlate);

        var parkingSession = await _parkingSessionRepository.GetActiveByLicensePlateAsync(
            normalizedLicensePlate,
            cancellationToken);

        parkingSession = EnsureActiveSessionExists(parkingSession);

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

        if (parkingSpot.SectorCode != parkingSession.SectorCode)
        {
            throw new DomainException("Parking spot sector does not match session sector.");
        }

        parkingSpot.Occupy();
        parkingSession.AssignParkingSpot(parkingSpot.Id, parkingSpot.SectorCode);

        var vehicleEvent = VehicleEventFactory.Create(
        ParkingEventType.Parked,
        normalizedLicensePlate,
        new {
            event_type = "PARKED",
            license_plate = command.LicensePlate,
            lat = command.Latitude,
            lng = command.Longitude,
            sector = parkingSession.SectorCode,
            spot_id = parkingSpot.Id
        });

        await AddVehicleEventAsync(vehicleEvent, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }
}