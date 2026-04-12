using System.Text.Json.Serialization;

namespace Estapar.Parking.Application.Contracts.Integrations;

public sealed record GarageSectorDto(
    [property: JsonPropertyName("sector")]
    string Sector,

    [property: JsonPropertyName("basePrice")]
    decimal BasePrice,

    [property: JsonPropertyName("max_capacity")]
    int MaxCapacity);