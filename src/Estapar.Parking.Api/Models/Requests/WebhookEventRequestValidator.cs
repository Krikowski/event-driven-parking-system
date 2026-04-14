namespace Estapar.Parking.Api.Models.Requests;

public static class WebhookEventRequestValidator
{
    public static IReadOnlyCollection<string> Validate(WebhookEventRequest? request)
    {
        var errors = new List<string>();

        if (request is null)
        {
            errors.Add("Request body is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            errors.Add("Event type is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            errors.Add("License plate is required.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var eventType = NormalizeEventType(request.EventType);

        switch (eventType)
        {
            case "ENTRY":
                if (!request.EntryTime.HasValue)
                {
                    errors.Add("Entry time is required for ENTRY events.");
                }

                break;

            case "PARKED":
                if (!request.Lat.HasValue || !request.Lng.HasValue)
                {
                    errors.Add("Latitude and longitude are required for PARKED events.");
                }

                break;

            case "EXIT":
                if (!request.ExitTime.HasValue)
                {
                    errors.Add("Exit time is required for EXIT events.");
                }

                break;

            default:
                errors.Add("Unsupported event type.");
                break;
        }

        return errors;
    }

    public static string NormalizeEventType(string eventType)
    {
        return eventType.Trim().ToUpperInvariant();
    }
}
