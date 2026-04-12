namespace Estapar.Parking.Infrastructure.Integrations;

public sealed class GarageApiOptions
{
    public const string SectionName = "GarageApi";

    public string BaseUrl { get; init; } = string.Empty;
}