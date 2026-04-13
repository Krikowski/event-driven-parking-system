using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Revenue;
using Estapar.Parking.Application.UseCases.Revenue;

namespace Estapar.Parking.UnitTests.Application.UseCases.Revenue;

public class GetRevenueUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnCorrectRevenueAmount()
    {
        var revenueReadRepository = new FakeRevenueReadRepository(125.50m);
        var useCase = new GetRevenueUseCase(revenueReadRepository);

        var query = new GetRevenueQuery("A", new DateOnly(2025, 1, 1));

        var beforeExecution = DateTime.UtcNow;
        var result = await useCase.ExecuteAsync(query);
        var afterExecution = DateTime.UtcNow;

        Assert.Equal(125.50m, result.Amount);
        Assert.Equal("A", revenueReadRepository.LastSectorCode);
        Assert.Equal(new DateOnly(2025, 1, 1), revenueReadRepository.LastDate);
        Assert.InRange(result.GeneratedAtUtc, beforeExecution, afterExecution);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterByDate()
    {
        var revenueReadRepository = new FakeRevenueReadRepository(80m);
        var useCase = new GetRevenueUseCase(revenueReadRepository);

        var query = new GetRevenueQuery("A", new DateOnly(2025, 2, 15));

        await useCase.ExecuteAsync(query);

        Assert.Equal(new DateOnly(2025, 2, 15), revenueReadRepository.LastDate);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeAndFilterBySector()
    {
        var revenueReadRepository = new FakeRevenueReadRepository(50m);
        var useCase = new GetRevenueUseCase(revenueReadRepository);

        var query = new GetRevenueQuery(" a ", new DateOnly(2025, 1, 1));

        var result = await useCase.ExecuteAsync(query);

        Assert.Equal(50m, result.Amount);
        Assert.Equal("A", revenueReadRepository.LastSectorCode);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnZero_WhenNoRevenueIsFound()
    {
        var revenueReadRepository = new FakeRevenueReadRepository(0m);
        var useCase = new GetRevenueUseCase(revenueReadRepository);

        var query = new GetRevenueQuery("B", new DateOnly(2025, 1, 1));

        var result = await useCase.ExecuteAsync(query);

        Assert.Equal(0m, result.Amount);
        Assert.Equal("B", revenueReadRepository.LastSectorCode);
        Assert.Equal(new DateOnly(2025, 1, 1), revenueReadRepository.LastDate);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnCurrencyAndUtcTimestamp()
    {
        var revenueReadRepository = new FakeRevenueReadRepository(200m);
        var useCase = new GetRevenueUseCase(revenueReadRepository);

        var query = new GetRevenueQuery("A", new DateOnly(2025, 1, 1));

        var beforeExecution = DateTime.UtcNow;
        var result = await useCase.ExecuteAsync(query);
        var afterExecution = DateTime.UtcNow;

        Assert.Equal("BRL", result.Currency);
        Assert.InRange(result.GeneratedAtUtc, beforeExecution, afterExecution);
        Assert.Equal(DateTimeKind.Utc, result.GeneratedAtUtc.Kind);
    }

    private sealed class FakeRevenueReadRepository : IRevenueReadRepository
    {
        private readonly decimal _amountToReturn;

        public FakeRevenueReadRepository(decimal amountToReturn)
        {
            _amountToReturn = amountToReturn;
        }

        public string? LastSectorCode { get; private set; }
        public DateOnly? LastDate { get; private set; }

        public Task<decimal> GetRevenueAmountAsync(
            string sectorCode,
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            LastSectorCode = sectorCode;
            LastDate = date;

            return Task.FromResult(_amountToReturn);
        }
    }
}