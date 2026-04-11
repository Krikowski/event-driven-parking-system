namespace Estapar.Parking.Application.Contracts.Integrations;

public sealed record GarageConfigurationDto(
    IReadOnlyCollection<GarageSectorDto> Sectors,
    IReadOnlyCollection<GarageSpotDto> Spots);