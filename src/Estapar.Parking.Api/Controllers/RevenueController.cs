using Estapar.Parking.Api.Models.Requests;
using Estapar.Parking.Api.Models.Responses;
using Estapar.Parking.Application.Contracts.Revenue;
using Estapar.Parking.Application.UseCases.Revenue;

using Microsoft.AspNetCore.Mvc;

namespace Estapar.Parking.Api.Controllers;

[ApiController]
[Route("revenue")]
public sealed class RevenueController : ControllerBase
{
    private readonly IGetRevenueUseCase _getRevenueUseCase;

    public RevenueController(IGetRevenueUseCase getRevenueUseCase)
    {
        _getRevenueUseCase = getRevenueUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RevenueResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(
        [FromQuery(Name = "sector")] string? sectorCode,
        [FromQuery(Name = "date")] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var validationErrors = RevenueQueryRequestValidator.Validate(sectorCode, date);

        if (validationErrors.Count > 0)
        {
            return BadRequest(new ErrorResponseModel
            {
                Code = "invalid_request",
                Message = "Revenue query validation failed.",
                Details = validationErrors,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var query = new GetRevenueQuery(
            sectorCode!,
            date!.Value);

        var result = await _getRevenueUseCase.ExecuteAsync(query, cancellationToken);

        var response = new RevenueResponseModel
        {
            Amount = result.Amount,
            Currency = result.Currency,
            Timestamp = result.GeneratedAtUtc
        };

        return Ok(response);
    }
}
