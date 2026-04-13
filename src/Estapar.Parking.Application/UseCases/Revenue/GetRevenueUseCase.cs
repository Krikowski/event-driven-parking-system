using Estapar.Parking.Application.Abstractions.Persistence;
using Estapar.Parking.Application.Contracts.Revenue;
using Estapar.Parking.Domain.Exceptions;

namespace Estapar.Parking.Application.UseCases.Revenue;

public sealed class GetRevenueUseCase : IGetRevenueUseCase
{
    private readonly IRevenueReadRepository _revenueReadRepository;

    public GetRevenueUseCase(IRevenueReadRepository revenueReadRepository)
    {
        _revenueReadRepository = revenueReadRepository;
    }

    public async Task<RevenueResultDto> ExecuteAsync(
        GetRevenueQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalizedSectorCode = NormalizeSectorCode(query.SectorCode);

        var revenueAmount = await _revenueReadRepository.GetRevenueAmountAsync(
            normalizedSectorCode,
            query.Date,
            cancellationToken);

        return new RevenueResultDto(
            revenueAmount,
            "BRL",
            DateTime.UtcNow);
    }

    private static string NormalizeSectorCode(string sectorCode)
    {
        if (string.IsNullOrWhiteSpace(sectorCode))
        {
            throw new DomainException("Sector code is required.");
        }

        return sectorCode.Trim().ToUpperInvariant();
    }
}