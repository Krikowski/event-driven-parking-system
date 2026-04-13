using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Common.Webhooks;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Policies;

namespace Estapar.Parking.Application.UseCases.Exit;

public sealed class HandleExitEventUseCase : WebhookUseCaseBase, IHandleExitEventUseCase
{
    private readonly IParkingSessionRepository _parkingSessionRepository;
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IPricingPolicy _pricingPolicy;

    public HandleExitEventUseCase(
        IParkingSessionRepository parkingSessionRepository,
        IParkingSpotRepository parkingSpotRepository,
        ISectorRepository sectorRepository,
        IVehicleEventRepository vehicleEventRepository,
        IPricingPolicy pricingPolicy,
        IUnitOfWork unitOfWork)
        : base(vehicleEventRepository, unitOfWork)
    {
        _parkingSessionRepository = parkingSessionRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _sectorRepository = sectorRepository;
        _pricingPolicy = pricingPolicy;
    }

    public async Task ExecuteAsync(
        HandleExitEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedLicensePlate = NormalizeLicensePlate(command.LicensePlate);

        var parkingSession = await _parkingSessionRepository.GetActiveByLicensePlateAsync(
            normalizedLicensePlate,
            cancellationToken);

        parkingSession = EnsureActiveSessionExists(parkingSession);

        var chargedAmount = _pricingPolicy.CalculateChargedAmount(
            parkingSession.EntryTimeUtc,
            command.ExitTimeUtc,
            parkingSession.FrozenHourlyRate);

        parkingSession.Close(command.ExitTimeUtc, chargedAmount);

        var sector = await _sectorRepository.GetByCodeAsync(parkingSession.SectorCode, cancellationToken);

        if (sector is null)
        {
            throw new InvalidOperationException(
                $"Sector '{parkingSession.SectorCode}' was not found for the active parking session.");
        }

        sector.ReleaseCapacity();

        if (parkingSession.ParkingSpotId.HasValue)
        {
            var parkingSpot = await _parkingSpotRepository.GetByIdAsync(
                parkingSession.ParkingSpotId.Value,
                cancellationToken);

            if (parkingSpot is not null)
            {
                parkingSpot.Release();
            }
        }

        var vehicleEvent = VehicleEventFactory.Create(
            ParkingEventType.Exit,
            normalizedLicensePlate,
            new {
                event_type = "EXIT",
                license_plate = command.LicensePlate,
                exit_time = command.ExitTimeUtc,
                sector = parkingSession.SectorCode,
                spot_id = parkingSession.ParkingSpotId, // ← ADICIONAR
                charged_amount = parkingSession.ChargedAmount
            });

        await AddVehicleEventAsync(vehicleEvent, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }
}