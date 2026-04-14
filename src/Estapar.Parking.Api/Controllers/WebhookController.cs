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
        var validationError = WebhookEventRequestValidator.Validate(request);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var eventType = WebhookEventRequestValidator.NormalizeEventType(request.EventType);

        switch (eventType)
        {
            case "ENTRY":
                await _handleEntryEventUseCase.ExecuteAsync(
                    new HandleEntryEventCommand(
                        request.LicensePlate,
                        request.EntryTime!.Value),
                    cancellationToken);
                break;

            case "PARKED":
                await _handleParkedEventUseCase.ExecuteAsync(
                    new HandleParkedEventCommand(
                        request.LicensePlate,
                        request.Lat!.Value,
                        request.Lng!.Value),
                    cancellationToken);
                break;

            case "EXIT":
                await _handleExitEventUseCase.ExecuteAsync(
                    new HandleExitEventCommand(
                        request.LicensePlate,
                        request.ExitTime!.Value),
                    cancellationToken);
                break;
        }

        return Ok();
    }
}