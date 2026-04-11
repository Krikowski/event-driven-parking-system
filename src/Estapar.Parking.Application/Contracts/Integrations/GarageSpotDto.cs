namespace Estapar.Parking.Application.Contracts.Integrations;

public sealed record GarageSpotDto(
    int Id,
    string Sector,
    decimal Lat,
    decimal Lng);