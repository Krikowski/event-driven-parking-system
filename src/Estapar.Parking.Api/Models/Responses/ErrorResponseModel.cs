namespace Estapar.Parking.Api.Models.Responses;

public sealed class ErrorResponseModel
{
    public string Code { get; init; } = default!;

    public string Message { get; init; } = default!;

    public string TraceId { get; init; } = default!;

    public DateTime Timestamp { get; init; }
}