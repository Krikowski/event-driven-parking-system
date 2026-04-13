using Estapar.Parking.Api.Models.Requests;
using Estapar.Parking.Application.Contracts.Webhooks;
using Estapar.Parking.Application.UseCases.Entry;
using Estapar.Parking.Application.UseCases.Exit;
using Estapar.Parking.Application.UseCases.Parked;
using Microsoft.AspNetCore.Mvc;

namespace Estapar.Parking.Api.Controllers;

[ApiController]
[Route("webhook")]
public sealed class WebhookController : ControllerBase
{
    private readonly IHandleEntryEventUseCase _handleEntryEventUseCase;
    private readonly IHandleParkedEventUseCase _handleParkedEventUseCase;
    private readonly IHandleExitEventUseCase _handleExitEventUseCase;

    public WebhookController(
        IHandleEntryEventUseCase handleEntryEventUseCase,
        IHandleParkedEventUseCase handleParkedEventUseCase,
        IHandleExitEventUseCase handleExitEventUseCase)
    {
        _handleEntryEventUseCase = handleEntryEventUseCase;
        _handleParkedEventUseCase = handleParkedEventUseCase;
        _handleExitEventUseCase = handleExitEventUseCase;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(
        [FromBody] WebhookEventRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return BadRequest("Event type is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            return BadRequest("License plate is required.");
        }

        var eventType = request.EventType.Trim().ToUpperInvariant();

        switch (eventType)
        {
            case "ENTRY":
                if (!request.EntryTime.HasValue)
                {
                    return BadRequest("Entry time is required for ENTRY events.");
                }

                await _handleEntryEventUseCase.ExecuteAsync(
                    new HandleEntryEventCommand(
                        request.LicensePlate,
                        request.EntryTime.Value),
                    cancellationToken);

                break;

            case "PARKED":
                if (!request.Lat.HasValue || !request.Lng.HasValue)
                {
                    return BadRequest("Latitude and longitude are required for PARKED events.");
                }

                await _handleParkedEventUseCase.ExecuteAsync(
                    new HandleParkedEventCommand(
                        request.LicensePlate,
                        request.Lat.Value,
                        request.Lng.Value),
                    cancellationToken);

                break;

            case "EXIT":
                if (!request.ExitTime.HasValue)
                {
                    return BadRequest("Exit time is required for EXIT events.");
                }

                await _handleExitEventUseCase.ExecuteAsync(
                    new HandleExitEventCommand(
                        request.LicensePlate,
                        request.ExitTime.Value),
                    cancellationToken);

                break;

            default:
                return BadRequest("Unsupported event type.");
        }

        return Ok();
    }
}