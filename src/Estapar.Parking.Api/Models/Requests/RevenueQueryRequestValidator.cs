namespace Estapar.Parking.Api.Models.Requests;

public static class RevenueQueryRequestValidator
{
    public static IReadOnlyCollection<string> Validate(string? sectorCode, DateOnly? date)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sectorCode))
        {
            errors.Add("Sector is required.");
        }

        if (!date.HasValue)
        {
            errors.Add("Date is required.");
        }

        return errors;
    }
}
