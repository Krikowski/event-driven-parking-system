using Estapar.Parking.Application.Contracts.Revenue;

namespace Estapar.Parking.Application.UseCases.Revenue;

public interface IGetRevenueUseCase
{
    Task<RevenueResultDto> ExecuteAsync(
        GetRevenueQuery query,
        CancellationToken cancellationToken = default);
}