namespace Estapar.Parking.Application.Contracts.Integrations;

public sealed record GarageSectorDto(
    string Sector,
    decimal BasePrice,
    int MaxCapacity);