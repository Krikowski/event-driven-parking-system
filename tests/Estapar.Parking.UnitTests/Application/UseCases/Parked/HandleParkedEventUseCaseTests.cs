using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Application.UseCases.Parked;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.UnitTests.Application.UseCases.Parked;

public class HandleParkedEventUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldThrowDomainException_WhenActiveSessionDoesNotExist()
    {
        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession: null);
        var parkingSpotRepository = new FakeParkingSpotRepository();
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleParkedEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            vehicleEventRepository,
            unitOfWork);

        var command = new HandleParkedEventCommand(
            "zul0001",
            -23.561684m,
            -46.655981m);

        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("No active parking session was found for this license plate.", exception.Message);
        Assert.Empty(vehicleEventRepository.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowDomainException_WhenParkingSpotIsNotFound()
    {
        var activeSession = CreateActiveSession("ZUL0001", "A");

        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession);
        var parkingSpotRepository = new FakeParkingSpotRepository(parkingSpot: null);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleParkedEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            vehicleEventRepository,
            unitOfWork);

        var command = new HandleParkedEventCommand(
            "zul0001",
            -23.561684m,
            -46.655981m);

        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("Parking spot was not found for the provided coordinates.", exception.Message);
        Assert.Null(activeSession.ParkingSpotId);
        Assert.Empty(vehicleEventRepository.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowDomainException_WhenParkingSpotIsAlreadyOccupied()
    {
        var activeSession = CreateActiveSession("ZUL0001", "A");
        var occupiedSpot = CreateOccupiedParkingSpot(id: 1, sectorCode: "A");

        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession);
        var parkingSpotRepository = new FakeParkingSpotRepository(occupiedSpot);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleParkedEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            vehicleEventRepository,
            unitOfWork);

        var command = new HandleParkedEventCommand(
            "zul0001",
            -23.561684m,
            -46.655981m);

        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("Parking spot is already occupied.", exception.Message);
        Assert.Null(activeSession.ParkingSpotId);
        Assert.Empty(vehicleEventRepository.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowDomainException_WhenParkingSpotBelongsToDifferentSector()
    {
        var activeSession = CreateActiveSession("ZUL0001", "A");
        var parkingSpot = new ParkingSpot(
            id: 1,
            sectorCode: "B",
            latitude: -23.561684m,
            longitude: -46.655981m);

        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession);
        var parkingSpotRepository = new FakeParkingSpotRepository(parkingSpot);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleParkedEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            vehicleEventRepository,
            unitOfWork);

        var command = new HandleParkedEventCommand(
            "zul0001",
            -23.561684m,
            -46.655981m);

        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("Parking spot sector does not match session sector.", exception.Message);
        Assert.Null(activeSession.ParkingSpotId);
        Assert.False(parkingSpot.IsOccupied);
        Assert.Empty(vehicleEventRepository.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAssignParkingSpotAndPersistEvent_WhenAssociationIsSuccessful()
    {
        var activeSession = CreateActiveSession("ZUL0001", "A");
        var parkingSpot = new ParkingSpot(
            id: 1,
            sectorCode: "A",
            latitude: -23.561684m,
            longitude: -46.655981m);

        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession);
        var parkingSpotRepository = new FakeParkingSpotRepository(parkingSpot);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleParkedEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            vehicleEventRepository,
            unitOfWork);

        var command = new HandleParkedEventCommand(
            " zul0001 ",
            -23.561684m,
            -46.655981m);

        await useCase.ExecuteAsync(command);

        var createdEvent = Assert.Single(vehicleEventRepository.AddedEvents);

        Assert.Equal(1, activeSession.ParkingSpotId);
        Assert.True(parkingSpot.IsOccupied);
        Assert.Equal("ZUL0001", createdEvent.LicensePlate);
        Assert.Equal(ParkingEventType.Parked, createdEvent.EventType);
        Assert.Contains("\"event_type\":\"PARKED\"", createdEvent.PayloadSnapshot);
        Assert.Contains("\"license_plate\":\" zul0001 \"", createdEvent.PayloadSnapshot);
        Assert.Contains("\"spot_id\":1", createdEvent.PayloadSnapshot);
        Assert.Contains("\"sector\":\"A\"", createdEvent.PayloadSnapshot);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static ParkingSession CreateActiveSession(string licensePlate, string sectorCode)
    {
        return new ParkingSession(
            licensePlate,
            sectorCode,
            CreateUtcDate(2025, 1, 1, 12, 0, 0),
            10m);
    }

    private static ParkingSpot CreateOccupiedParkingSpot(int id, string sectorCode)
    {
        var spot = new ParkingSpot(
            id,
            sectorCode,
            -23.561684m,
            -46.655981m);

        spot.Occupy();

        return spot;
    }

    private static DateTime CreateUtcDate(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }

    private sealed class FakeParkingSessionRepository : IParkingSessionRepository
    {
        private readonly ParkingSession? _activeSession;

        public FakeParkingSessionRepository(ParkingSession? activeSession)
        {
            _activeSession = activeSession;
        }

        public List<ParkingSession> AddedSessions { get; } = new();

        public Task<bool> ExistsActiveByLicensePlateAsync(
            string licensePlate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_activeSession is not null);
        }

        public Task<ParkingSession?> GetActiveByLicensePlateAsync(
            string licensePlate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_activeSession);
        }

        public Task AddAsync(ParkingSession parkingSession, CancellationToken cancellationToken = default)
        {
            AddedSessions.Add(parkingSession);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeParkingSpotRepository : IParkingSpotRepository
    {
        private readonly ParkingSpot? _parkingSpot;

        public FakeParkingSpotRepository(ParkingSpot? parkingSpot = null)
        {
            _parkingSpot = parkingSpot;
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_parkingSpot is not null);
        }

        public Task<ParkingSpot?> GetByCoordinatesAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_parkingSpot);
        }

        public Task<IReadOnlyCollection<ParkingSpot>> GetBySectorCodeAsync(
            string sectorCode,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ParkingSpot> parkingSpots = _parkingSpot is null
                ? Array.Empty<ParkingSpot>()
                : new[] { _parkingSpot };

            return Task.FromResult(parkingSpots);
        }

        public Task AddRangeAsync(IEnumerable<ParkingSpot> parkingSpots, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeVehicleEventRepository : IVehicleEventRepository
    {
        public List<VehicleEvent> AddedEvents { get; } = new();

        public Task AddAsync(VehicleEvent vehicleEvent, CancellationToken cancellationToken = default)
        {
            AddedEvents.Add(vehicleEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}