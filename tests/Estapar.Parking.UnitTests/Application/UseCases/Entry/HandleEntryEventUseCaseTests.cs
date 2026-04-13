using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Application.UseCases.Entry;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Domain.Exceptions;
using Estapar.Parking.Domain.Policies;

using Microsoft.Extensions.Logging.Abstractions;

namespace Estapar.Parking.UnitTests.Application.UseCases.Entry;

public class HandleEntryEventUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldThrowDomainException_WhenActiveSessionAlreadyExists()
    {
        var parkingSessionRepository = new FakeParkingSessionRepository(existsActiveSession: true);
        var sectorRepository = new FakeSectorRepository();
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleEntryEventUseCase(
            parkingSessionRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleEntryEventUseCase>.Instance);

        var command = new HandleEntryEventCommand("zul0001", DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));

        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("An active parking session already exists for this license plate.", exception.Message);
        Assert.Empty(parkingSessionRepository.AddedSessions);
        Assert.Empty(vehicleEventRepository.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowDomainException_WhenParkingLotIsFull()
    {
        var sectors = new[]
        {
            CreateSector("A", maxCapacity: 2, allocatedCapacity: 2, basePrice: 10m),
            CreateSector("B", maxCapacity: 1, allocatedCapacity: 1, basePrice: 10m)
        };

        var parkingSessionRepository = new FakeParkingSessionRepository(existsActiveSession: false);
        var sectorRepository = new FakeSectorRepository(sectors);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleEntryEventUseCase(
            parkingSessionRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleEntryEventUseCase>.Instance);

        var command = new HandleEntryEventCommand("zul0001", CreateUtcDate(2025, 1, 1, 12, 0, 0));

        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("Parking lot is full.", exception.Message);
        Assert.Empty(parkingSessionRepository.AddedSessions);
        Assert.Empty(vehicleEventRepository.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSelectSectorWithLowestOccupancyPercentage()
    {
        var sectors = new[]
        {
            CreateSector("A", maxCapacity: 10, allocatedCapacity: 5, basePrice: 10m),
            CreateSector("B", maxCapacity: 10, allocatedCapacity: 2, basePrice: 10m)
        };

        var parkingSessionRepository = new FakeParkingSessionRepository(existsActiveSession: false);
        var sectorRepository = new FakeSectorRepository(sectors);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleEntryEventUseCase(
            parkingSessionRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleEntryEventUseCase>.Instance);

        var command = new HandleEntryEventCommand("zul0001", CreateUtcDate(2025, 1, 1, 12, 0, 0));

        await useCase.ExecuteAsync(command);

        var createdSession = Assert.Single(parkingSessionRepository.AddedSessions);

        Assert.Equal("ZUL0001", createdSession.LicensePlate);
        Assert.Equal("B", createdSession.SectorCode);
        Assert.Equal(9.0m, createdSession.FrozenHourlyRate);
        Assert.Equal(5, sectors.Single(s => s.Code == "A").AllocatedCapacity);
        Assert.Equal(3, sectors.Single(s => s.Code == "B").AllocatedCapacity);
        Assert.Single(vehicleEventRepository.AddedEvents);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSelectSectorWithLowestBasePrice_WhenOccupancyIsTied()
    {
        var sectors = new[]
        {
            CreateSector("A", maxCapacity: 10, allocatedCapacity: 2, basePrice: 12m),
            CreateSector("B", maxCapacity: 10, allocatedCapacity: 2, basePrice: 10m)
        };

        var parkingSessionRepository = new FakeParkingSessionRepository(existsActiveSession: false);
        var sectorRepository = new FakeSectorRepository(sectors);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleEntryEventUseCase(
            parkingSessionRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleEntryEventUseCase>.Instance);

        var command = new HandleEntryEventCommand("zul0001", CreateUtcDate(2025, 1, 1, 12, 0, 0));

        await useCase.ExecuteAsync(command);

        var createdSession = Assert.Single(parkingSessionRepository.AddedSessions);

        Assert.Equal("B", createdSession.SectorCode);
        Assert.Equal(9.0m, createdSession.FrozenHourlyRate);
        Assert.Equal(2, sectors.Single(s => s.Code == "A").AllocatedCapacity);
        Assert.Equal(3, sectors.Single(s => s.Code == "B").AllocatedCapacity);
        Assert.Single(vehicleEventRepository.AddedEvents);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSelectSectorByCode_WhenOccupancyAndBasePriceAreTied()
    {
        var sectors = new[]
        {
            CreateSector("B", maxCapacity: 10, allocatedCapacity: 2, basePrice: 10m),
            CreateSector("A", maxCapacity: 10, allocatedCapacity: 2, basePrice: 10m)
        };

        var parkingSessionRepository = new FakeParkingSessionRepository(existsActiveSession: false);
        var sectorRepository = new FakeSectorRepository(sectors);
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleEntryEventUseCase(
            parkingSessionRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleEntryEventUseCase>.Instance);

        var command = new HandleEntryEventCommand("zul0001", CreateUtcDate(2025, 1, 1, 12, 0, 0));

        await useCase.ExecuteAsync(command);

        var createdSession = Assert.Single(parkingSessionRepository.AddedSessions);

        Assert.Equal("A", createdSession.SectorCode);
        Assert.Equal(9.0m, createdSession.FrozenHourlyRate);
        Assert.Equal(3, sectors.Single(s => s.Code == "A").AllocatedCapacity);
        Assert.Equal(2, sectors.Single(s => s.Code == "B").AllocatedCapacity);
        Assert.Single(vehicleEventRepository.AddedEvents);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(0, 100, 9.0)]
    [InlineData(25, 100, 10.0)]
    [InlineData(50, 100, 10.0)]
    [InlineData(75, 100, 11.0)]
    public async Task ExecuteAsync_ShouldFreezeCorrectHourlyRate_AtOccupancyBoundaries(
        int allocatedCapacityBeforeEntry,
        int maxCapacity,
        decimal expectedFrozenHourlyRate)
    {
        var sector = CreateSector("A", maxCapacity, allocatedCapacityBeforeEntry, 10m);

        var parkingSessionRepository = new FakeParkingSessionRepository(existsActiveSession: false);
        var sectorRepository = new FakeSectorRepository(new[] { sector });
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleEntryEventUseCase(
            parkingSessionRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleEntryEventUseCase>.Instance);

        var command = new HandleEntryEventCommand("zul0001", CreateUtcDate(2025, 1, 1, 12, 0, 0));

        await useCase.ExecuteAsync(command);

        var createdSession = Assert.Single(parkingSessionRepository.AddedSessions);

        Assert.Equal(expectedFrozenHourlyRate, createdSession.FrozenHourlyRate);
        Assert.Single(vehicleEventRepository.AddedEvents);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeLicensePlate_AndPersistEntryEvent()
    {
        var sector = CreateSector("A", maxCapacity: 10, allocatedCapacity: 0, basePrice: 10m);

        var parkingSessionRepository = new FakeParkingSessionRepository(existsActiveSession: false);
        var sectorRepository = new FakeSectorRepository(new[] { sector });
        var vehicleEventRepository = new FakeVehicleEventRepository();
        var pricingPolicy = new PricingPolicy();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new HandleEntryEventUseCase(
            parkingSessionRepository,
            sectorRepository,
            vehicleEventRepository,
            pricingPolicy,
            unitOfWork,
            NullLogger<HandleEntryEventUseCase>.Instance);

        var command = new HandleEntryEventCommand(" zul0001 ", CreateUtcDate(2025, 1, 1, 12, 0, 0));

        await useCase.ExecuteAsync(command);

        var createdSession = Assert.Single(parkingSessionRepository.AddedSessions);
        var createdEvent = Assert.Single(vehicleEventRepository.AddedEvents);

        Assert.Equal("ZUL0001", createdSession.LicensePlate);
        Assert.Equal("ZUL0001", createdEvent.LicensePlate);
        Assert.Equal(ParkingEventType.Entry, createdEvent.EventType);
        Assert.Contains("\"event_type\":\"ENTRY\"", createdEvent.PayloadSnapshot);
        Assert.Contains("\"license_plate\":\" zul0001 \"", createdEvent.PayloadSnapshot);
        Assert.Contains("\"sector\":\"A\"", createdEvent.PayloadSnapshot);
        Assert.Contains("\"frozen_hourly_rate\":9.0", createdEvent.PayloadSnapshot);
        Assert.Equal(1, sector.AllocatedCapacity);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static Sector CreateSector(string code, int maxCapacity, int allocatedCapacity, decimal basePrice)
    {
        var sector = new Sector(code, maxCapacity, basePrice);

        for (var i = 0; i < allocatedCapacity; i++)
        {
            sector.ConsumeCapacity();
        }

        return sector;
    }

    private static DateTime CreateUtcDate(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }

    private sealed class FakeParkingSessionRepository : IParkingSessionRepository
    {
        private readonly bool _existsActiveSession;

        public FakeParkingSessionRepository(bool existsActiveSession)
        {
            _existsActiveSession = existsActiveSession;
        }

        public List<ParkingSession> AddedSessions { get; } = new();

        public Task<bool> ExistsActiveByLicensePlateAsync(
            string licensePlate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_existsActiveSession);
        }

        public Task<ParkingSession?> GetActiveByLicensePlateAsync(
            string licensePlate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ParkingSession?>(null);
        }

        public Task AddAsync(ParkingSession parkingSession, CancellationToken cancellationToken = default)
        {
            AddedSessions.Add(parkingSession);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSectorRepository : ISectorRepository
    {
        private readonly IReadOnlyCollection<Sector> _sectors;

        public FakeSectorRepository(IReadOnlyCollection<Sector>? sectors = null)
        {
            _sectors = sectors ?? Array.Empty<Sector>();
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_sectors.Count > 0);
        }

        public Task<Sector?> GetByCodeAsync(string sectorCode, CancellationToken cancellationToken = default)
        {
            var sector = _sectors.FirstOrDefault(s => s.Code == sectorCode);
            return Task.FromResult(sector);
        }

        public Task<IReadOnlyCollection<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_sectors);
        }

        public Task AddRangeAsync(IEnumerable<Sector> sectors, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeVehicleEventRepository : IVehicleEventRepository
    {
        private readonly HashSet<string> _idempotencyKeys = new();

        public List<VehicleEvent> AddedEvents { get; } = new();

        public Task<bool> ExistsByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_idempotencyKeys.Contains(idempotencyKey));
        }

        public Task AddAsync(VehicleEvent vehicleEvent, CancellationToken cancellationToken = default)
        {
            AddedEvents.Add(vehicleEvent);
            _idempotencyKeys.Add(vehicleEvent.IdempotencyKey);

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