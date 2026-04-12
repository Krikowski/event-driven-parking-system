using System.Text.Json.Serialization;

namespace Estapar.Parking.Application.Contracts.Integrations;

public sealed record GarageSpotDto(
    [property: JsonPropertyName("id")]
    int Id,

    [property: JsonPropertyName("sector")]
    string Sector,

    [property: JsonPropertyName("lat")]
    decimal Latitude,

    [property: JsonPropertyName("lng")]
    decimal Longitude);