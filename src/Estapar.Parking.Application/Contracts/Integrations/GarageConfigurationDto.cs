using System.Text.Json.Serialization;

namespace Estapar.Parking.Application.Contracts.Integrations;

public sealed record GarageConfigurationDto(
    [property: JsonPropertyName("garage")]
    IReadOnlyCollection<GarageSectorDto> Sectors,

    [property: JsonPropertyName("spots")]
    IReadOnlyCollection<GarageSpotDto> Spots);