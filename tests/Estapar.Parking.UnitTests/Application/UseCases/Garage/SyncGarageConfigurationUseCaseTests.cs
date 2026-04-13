using Estapar.Parking.Application.Abstractions.Integrations;
using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Integrations;
using Estapar.Parking.Application.UseCases.Garage;
using Estapar.Parking.Domain.Entities;

using Microsoft.Extensions.Logging.Abstractions;

namespace Estapar.Parking.UnitTests.Application.UseCases.Garage;

public class SyncGarageConfigurationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldPersistSectorsAndParkingSpots_WhenDatabaseIsEmpty()
    {
        var garageConfigurationClient = new FakeGarageConfigurationClient(
            new GarageConfigurationDto(
                new List<GarageSectorDto>
                {
                    new("A", 10m, 100),
                    new("B", 20m, 50)
                },
                new List<GarageSpotDto>
                {
                    new(1, "A", -23.561684m, -46.655981m),
                    new(2, "B", -23.561685m, -46.655982m)
                }));

        var sectorRepository = new FakeSectorRepository(hasAny: false);
        var parkingSpotRepository = new FakeParkingSpotRepository(hasAny: false);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new SyncGarageConfigurationUseCase(
            garageConfigurationClient,
            sectorRepository,
            parkingSpotRepository,
            unitOfWork,
            NullLogger<SyncGarageConfigurationUseCase>.Instance);

        await useCase.ExecuteAsync();

        Assert.True(garageConfigurationClient.WasCalled);
        Assert.Equal(2, sectorRepository.AddedSectors.Count);
        Assert.Equal(2, parkingSpotRepository.AddedParkingSpots.Count);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        Assert.Contains(
            sectorRepository.AddedSectors,
            sector => sector.Code == "A" && sector.BasePrice == 10m && sector.MaxCapacity == 100);

        Assert.Contains(
            sectorRepository.AddedSectors,
            sector => sector.Code == "B" && sector.BasePrice == 20m && sector.MaxCapacity == 50);

        Assert.Contains(
            parkingSpotRepository.AddedParkingSpots,
            parkingSpot => parkingSpot.Id == 1 && parkingSpot.SectorCode == "A");

        Assert.Contains(
            parkingSpotRepository.AddedParkingSpots,
            parkingSpot => parkingSpot.Id == 2 && parkingSpot.SectorCode == "B");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSynchronize_WhenConfigurationAlreadyExists()
    {
        var garageConfigurationClient = new FakeGarageConfigurationClient(
            new GarageConfigurationDto(
                new List<GarageSectorDto>
                {
                    new("A", 10m, 100)
                },
                new List<GarageSpotDto>
                {
                    new(1, "A", -23.561684m, -46.655981m)
                }));

        var sectorRepository = new FakeSectorRepository(hasAny: true);
        var parkingSpotRepository = new FakeParkingSpotRepository(hasAny: true);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new SyncGarageConfigurationUseCase(
            garageConfigurationClient,
            sectorRepository,
            parkingSpotRepository,
            unitOfWork,
            NullLogger<SyncGarageConfigurationUseCase>.Instance);

        await useCase.ExecuteAsync();

        Assert.False(garageConfigurationClient.WasCalled);
        Assert.Empty(sectorRepository.AddedSectors);
        Assert.Empty(parkingSpotRepository.AddedParkingSpots);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowInvalidOperationException_WhenConfigurationIsPartiallyPersisted()
    {
        var garageConfigurationClient = new FakeGarageConfigurationClient(
            new GarageConfigurationDto(
                new List<GarageSectorDto>
                {
                    new("A", 10m, 100)
                },
                new List<GarageSpotDto>
                {
                    new(1, "A", -23.561684m, -46.655981m)
                }));

        var sectorRepository = new FakeSectorRepository(hasAny: true);
        var parkingSpotRepository = new FakeParkingSpotRepository(hasAny: false);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new SyncGarageConfigurationUseCase(
            garageConfigurationClient,
            sectorRepository,
            parkingSpotRepository,
            unitOfWork,
            NullLogger<SyncGarageConfigurationUseCase>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync());

        Assert.Equal(
            "Garage configuration is partially persisted. Manual intervention is required.",
            exception.Message);

        Assert.False(garageConfigurationClient.WasCalled);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowInvalidOperationException_WhenParkingSpotReferencesUnknownSector()
    {
        var garageConfigurationClient = new FakeGarageConfigurationClient(
            new GarageConfigurationDto(
                new List<GarageSectorDto>
                {
                    new("A", 10m, 100)
                },
                new List<GarageSpotDto>
                {
                    new(1, "B", -23.561684m, -46.655981m)
                }));

        var sectorRepository = new FakeSectorRepository(hasAny: false);
        var parkingSpotRepository = new FakeParkingSpotRepository(hasAny: false);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new SyncGarageConfigurationUseCase(
            garageConfigurationClient,
            sectorRepository,
            parkingSpotRepository,
            unitOfWork,
            NullLogger<SyncGarageConfigurationUseCase>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync());

        Assert.Equal(
            "Parking spot '1' references unknown sector 'B'.",
            exception.Message);

        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private sealed class FakeGarageConfigurationClient : IGarageConfigurationClient
    {
        private readonly GarageConfigurationDto _configuration;

        public FakeGarageConfigurationClient(GarageConfigurationDto configuration)
        {
            _configuration = configuration;
        }

        public bool WasCalled { get; private set; }

        public Task<GarageConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(_configuration);
        }
    }

    private sealed class FakeSectorRepository : ISectorRepository
    {
        private readonly bool _hasAny;

        public FakeSectorRepository(bool hasAny)
        {
            _hasAny = hasAny;
        }

        public List<Sector> AddedSectors { get; } = new();

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_hasAny);
        }

        public Task<Sector?> GetByCodeAsync(string sectorCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Sector?>(null);
        }

        public Task<IReadOnlyCollection<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Sector> sectors = Array.Empty<Sector>();
            return Task.FromResult(sectors);
        }

        public Task AddRangeAsync(IEnumerable<Sector> sectors, CancellationToken cancellationToken = default)
        {
            AddedSectors.AddRange(sectors);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeParkingSpotRepository : IParkingSpotRepository
    {
        private readonly bool _hasAny;

        public FakeParkingSpotRepository(bool hasAny)
        {
            _hasAny = hasAny;
        }

        public List<ParkingSpot> AddedParkingSpots { get; } = new();

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_hasAny);
        }

        public Task<ParkingSpot?> GetByIdAsync(
            int parkingSpotId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ParkingSpot?>(null);
        }

        public Task<ParkingSpot?> GetByCoordinatesAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ParkingSpot?>(null);
        }

        public Task<IReadOnlyCollection<ParkingSpot>> GetBySectorCodeAsync(
            string sectorCode,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ParkingSpot> parkingSpots = Array.Empty<ParkingSpot>();
            return Task.FromResult(parkingSpots);
        }

        public Task AddRangeAsync(IEnumerable<ParkingSpot> parkingSpots, CancellationToken cancellationToken = default)
        {
            AddedParkingSpots.AddRange(parkingSpots);
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