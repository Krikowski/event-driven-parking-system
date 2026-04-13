using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Application.Common.Webhooks;

public abstract class WebhookUseCaseBase
{
    private readonly IVehicleEventRepository _vehicleEventRepository;
    private readonly IUnitOfWork _unitOfWork;

    protected WebhookUseCaseBase(
        IVehicleEventRepository vehicleEventRepository,
        IUnitOfWork unitOfWork)
    {
        _vehicleEventRepository = vehicleEventRepository;
        _unitOfWork = unitOfWork;
    }

    protected static string NormalizeLicensePlate(string licensePlate)
    {
        return LicensePlateNormalizer.Normalize(licensePlate);
    }

    protected static ParkingSession EnsureActiveSessionExists(ParkingSession? parkingSession)
    {
        if (parkingSession is null)
        {
            throw new DomainException("No active parking session was found for this license plate.");
        }

        return parkingSession;
    }

    protected async Task AddVehicleEventAsync(
        VehicleEvent vehicleEvent,
        CancellationToken cancellationToken)
    {
        await _vehicleEventRepository.AddAsync(vehicleEvent, cancellationToken);
    }

    protected async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}