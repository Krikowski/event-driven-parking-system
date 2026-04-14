using Estapar.Parking.Api.Models.Requests;
using Estapar.Parking.Api.Models.Responses;
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
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IHandleEntryEventUseCase handleEntryEventUseCase,
        IHandleParkedEventUseCase handleParkedEventUseCase,
        IHandleExitEventUseCase handleExitEventUseCase,
        ILogger<WebhookController> logger)
    {
        _handleEntryEventUseCase = handleEntryEventUseCase;
        _handleParkedEventUseCase = handleParkedEventUseCase;
        _handleExitEventUseCase = handleExitEventUseCase;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(WebhookAcceptedResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseModel), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseModel), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponseModel), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post(
        [FromBody] WebhookEventRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = WebhookEventRequestValidator.Validate(request);

        if (validationErrors.Count > 0)
        {
            return BadRequest(new ErrorResponseModel
            {
                Code = "invalid_request",
                Message = "Webhook request validation failed.",
                Details = validationErrors,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var eventType = WebhookEventRequestValidator.NormalizeEventType(request.EventType);
        var normalizedLicensePlate = request.LicensePlate.Trim().ToUpperInvariant();

        _logger.LogInformation(
            "Webhook request accepted for processing. EventType: {EventType}, LicensePlate: {LicensePlate}, TraceId: {TraceId}",
            eventType,
            normalizedLicensePlate,
            HttpContext.TraceIdentifier);

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

        return Ok(new WebhookAcceptedResponseModel
        {
            Status = "processed",
            Message = $"Webhook event '{eventType}' processed successfully.",
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }
}
