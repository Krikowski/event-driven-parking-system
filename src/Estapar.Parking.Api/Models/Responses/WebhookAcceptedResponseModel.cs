namespace Estapar.Parking.Api.Models.Responses;

public sealed class WebhookAcceptedResponseModel
{
    public string Status { get; init; } = default!;

    public string Message { get; init; } = default!;

    public string TraceId { get; init; } = default!;

    public DateTime Timestamp { get; init; }
}
