namespace Estapar.Parking.Api.Models.Requests;

public static class WebhookEventRequestValidator
{
    public static string? Validate(WebhookEventRequest? request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return "Event type is required.";
        }

        if (string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            return "License plate is required.";
        }

        var eventType = NormalizeEventType(request.EventType);

        return eventType switch
        {
            "ENTRY" when !request.EntryTime.HasValue
                => "Entry time is required for ENTRY events.",

            "PARKED" when !request.Lat.HasValue || !request.Lng.HasValue
                => "Latitude and longitude are required for PARKED events.",

            "EXIT" when !request.ExitTime.HasValue
                => "Exit time is required for EXIT events.",

            "ENTRY" or "PARKED" or "EXIT"
                => null,

            _ => "Unsupported event type."
        };
    }

    public static string NormalizeEventType(string eventType)
    {
        return eventType.Trim().ToUpperInvariant();
    }
}