using Estapar.Parking.Application.Contracts.Integrations;

namespace Estapar.Parking.Application.Abstractions.Integrations;

public interface IGarageConfigurationClient
{
    Task<GarageConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);
}