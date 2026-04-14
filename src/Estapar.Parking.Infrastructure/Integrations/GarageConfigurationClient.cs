using System.Net.Http.Json;
using Estapar.Parking.Application.Abstractions.Integrations;
using Estapar.Parking.Application.Contracts.Integrations;

namespace Estapar.Parking.Infrastructure.Integrations;

public sealed class GarageConfigurationClient : IGarageConfigurationClient
{
    private readonly HttpClient _httpClient;

    public GarageConfigurationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GarageConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("garage", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await TryReadResponseContentAsync(response, cancellationToken);

                throw new InvalidOperationException(
                    $"Failed to fetch garage configuration. " +
                    $"Status code: {(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Response: {responseContent}");
            }

            var configuration = await response.Content.ReadFromJsonAsync<GarageConfigurationDto>(cancellationToken: cancellationToken);

            if (configuration is null)
            {
                throw new InvalidOperationException("Garage configuration response could not be deserialized.");
            }

            return configuration;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Timed out while fetching garage configuration.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("HTTP error while fetching garage configuration.", ex);
        }
    }

    private static async Task<string> TryReadResponseContentAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content is null)
        {
            return "<empty>";
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(content)
            ? "<empty>"
            : content;
    }
}
