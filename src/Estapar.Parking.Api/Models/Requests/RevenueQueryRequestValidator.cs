namespace Estapar.Parking.Api.Models.Requests;

public static class RevenueQueryRequestValidator
{
    public static string? Validate(string? sectorCode, DateOnly? date)
    {
        if (string.IsNullOrWhiteSpace(sectorCode))
        {
            return "Sector is required.";
        }

        if (!date.HasValue)
        {
            return "Date is required.";
        }

        return null;
    }
}