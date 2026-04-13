using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Application.UseCases.Exit;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;
using Estapar.Parking.Domain.Policies;

using Microsoft.Extensions.Logging.Abstractions;

namespace Estapar.Parking.UnitTests.Application.UseCases.Exit;

public class HandleExitEventUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldThrowDomainException_WhenActiveSessionDoesNotExist()
    {
        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession: null);
        var sectorRepository = new FakeSectorRepository();
        var parkingSpotRepository = new FakeParkingSpotRepository();
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleExitEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleExitEventUseCase>.Instance);

        var command = new HandleExitEventCommand(
            "zul0001",
            CreateUtcDate(2025, 1, 1, 12, 30, 0));

        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("No active parking session was found for this license plate.", exception.Message);
        Assert.Empty(vehicleEventRepository.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldChargeZero_WhenExitOccursWithinThirtyMinutes()
    {
        var activeSession = CreateActiveSession(
            licensePlate: "ZUL0001",
            sectorCode: "A",
            entryTimeUtc: CreateUtcDate(2025, 1, 1, 12, 0, 0),
            frozenHourlyRate: 10m);

        var sector = CreateSectorWithAllocatedCapacity("A", maxCapacity: 10, allocatedCapacity: 1);

        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession);
        var sectorRepository = new FakeSectorRepository(sector);
        var parkingSpotRepository = new FakeParkingSpotRepository();
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleExitEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleExitEventUseCase>.Instance);

        var command = new HandleExitEventCommand(
            "zul0001",
            CreateUtcDate(2025, 1, 1, 12, 30, 0));

        await useCase.ExecuteAsync(command);

        var createdEvent = Assert.Single(vehicleEventRepository.AddedEvents);

        Assert.True(activeSession.IsClosed);
        Assert.Equal(0m, activeSession.ChargedAmount);
        Assert.Equal(0, sector.AllocatedCapacity);
        Assert.Equal(ParkingEventType.Exit, createdEvent.EventType);
        Assert.Contains("\"charged_amount\":0", createdEvent.PayloadSnapshot);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRoundUpHourlyCharge_WhenExitExceedsWholeHours()
    {
        var activeSession = CreateActiveSession(
            licensePlate: "ZUL0001",
            sectorCode: "A",
            entryTimeUtc: CreateUtcDate(2025, 1, 1, 12, 0, 0),
            frozenHourlyRate: 10m);

        var sector = CreateSectorWithAllocatedCapacity("A", maxCapacity: 10, allocatedCapacity: 1);

        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession);
        var sectorRepository = new FakeSectorRepository(sector);
        var parkingSpotRepository = new FakeParkingSpotRepository();
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleExitEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleExitEventUseCase>.Instance);

        var command = new HandleExitEventCommand(
            "zul0001",
            CreateUtcDate(2025, 1, 1, 13, 1, 0));

        await useCase.ExecuteAsync(command);

        var createdEvent = Assert.Single(vehicleEventRepository.AddedEvents);

        Assert.True(activeSession.IsClosed);
        Assert.Equal(20m, activeSession.ChargedAmount);
        Assert.Equal(0, sector.AllocatedCapacity);
        Assert.Contains("\"charged_amount\":20", createdEvent.PayloadSnapshot);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReleaseAssignedParkingSpot_WhenSessionHasAssociatedSpot()
    {
        var activeSession = CreateActiveSession(
            licensePlate: "ZUL0001",
            sectorCode: "A",
            entryTimeUtc: CreateUtcDate(2025, 1, 1, 12, 0, 0),
            frozenHourlyRate: 10m);

        var assignedSpot = CreateOccupiedParkingSpot(id: 1, sectorCode: "A");
        activeSession.AssignParkingSpot(assignedSpot.Id, assignedSpot.SectorCode);

        var sector = CreateSectorWithAllocatedCapacity("A", maxCapacity: 10, allocatedCapacity: 1);

        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession);
        var sectorRepository = new FakeSectorRepository(sector);
        var parkingSpotRepository = new FakeParkingSpotRepository(assignedSpot);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleExitEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleExitEventUseCase>.Instance );

        var command = new HandleExitEventCommand(
            "zul0001",
            CreateUtcDate(2025, 1, 1, 12, 45, 0));

        await useCase.ExecuteAsync(command);

        var createdEvent = Assert.Single(vehicleEventRepository.AddedEvents);

        Assert.False(assignedSpot.IsOccupied);
        Assert.Equal(0, sector.AllocatedCapacity);
        Assert.True(activeSession.IsClosed);
        Assert.Equal(10m, activeSession.ChargedAmount);
        Assert.Contains("\"spot_id\":1", createdEvent.PayloadSnapshot);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistChargedAmountAndExitEvent_WhenExitIsSuccessful()
    {
        var activeSession = CreateActiveSession(
            licensePlate: "ZUL0001",
            sectorCode: "A",
            entryTimeUtc: CreateUtcDate(2025, 1, 1, 12, 0, 0),
            frozenHourlyRate: 12.5m);

        var sector = CreateSectorWithAllocatedCapacity("A", maxCapacity: 10, allocatedCapacity: 1);

        var parkingSessionRepository = new FakeParkingSessionRepository(activeSession);
        var sectorRepository = new FakeSectorRepository(sector);
        var parkingSpotRepository = new FakeParkingSpotRepository();
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleExitEventUseCase(
            parkingSessionRepository,
            parkingSpotRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleExitEventUseCase>.Instance );

        var command = new HandleExitEventCommand(
            " zul0001 ",
            CreateUtcDate(2025, 1, 1, 14, 5, 0));

        await useCase.ExecuteAsync(command);

        var createdEvent = Assert.Single(vehicleEventRepository.AddedEvents);

        Assert.True(activeSession.IsClosed);
        Assert.Equal(37.5m, activeSession.ChargedAmount);
        Assert.Equal("ZUL0001", createdEvent.LicensePlate);
        Assert.Equal(ParkingEventType.Exit, createdEvent.EventType);
        Assert.Contains("\"event_type\":\"EXIT\"", createdEvent.PayloadSnapshot);
        Assert.Contains("\"license_plate\":\" zul0001 \"", createdEvent.PayloadSnapshot);
        Assert.Contains("\"sector\":\"A\"", createdEvent.PayloadSnapshot);
        Assert.Contains("\"charged_amount\":37.5", createdEvent.PayloadSnapshot);
        Assert.Equal(0, sector.AllocatedCapacity);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static ParkingSession CreateActiveSession(
        string licensePlate,
        string sectorCode,
        DateTime entryTimeUtc,
        decimal frozenHourlyRate)
    {
        return new ParkingSession(
            licensePlate,
            sectorCode,
            entryTimeUtc,
            frozenHourlyRate);
    }

    private static Sector CreateSectorWithAllocatedCapacity(string code, int maxCapacity, int allocatedCapacity)
    {
        var sector = new Sector(code, maxCapacity, 10m);

        for (var i = 0; i < allocatedCapacity; i++)
        {
            sector.ConsumeCapacity();
        }

        return sector;
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

    private sealed class FakeSectorRepository : ISectorRepository
    {
        private readonly Sector? _sector;

        public FakeSectorRepository(Sector? sector = null)
        {
            _sector = sector;
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_sector is not null);
        }

        public Task<Sector?> GetByCodeAsync(string sectorCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_sector is not null && _sector.Code == sectorCode
                ? _sector
                : null);
        }

        public Task<IReadOnlyCollection<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Sector> sectors = _sector is null
                ? Array.Empty<Sector>()
                : new[] { _sector };

            return Task.FromResult(sectors);
        }

        public Task AddRangeAsync(IEnumerable<Sector> sectors, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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

        public Task<ParkingSpot?> GetByIdAsync(
            int parkingSpotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_parkingSpot is not null && _parkingSpot.Id == parkingSpotId
                ? _parkingSpot
                : null);
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