using System.Net;
using System.Text;
using Estapar.Parking.Infrastructure.Integrations;

namespace Estapar.Parking.UnitTests.Infrastructure.Integrations;

public sealed class GarageConfigurationClientTests
{
    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnGarageConfiguration_WhenResponseIsSuccessful()
    {
        const string responseContent = """
        {
          "garage": [
            {
              "sector": "A",
              "basePrice": 10.0,
              "max_capacity": 100
            }
          ],
          "spots": [
            {
              "id": 1,
              "sector": "A",
              "lat": -23.561684,
              "lng": -46.655981
            }
          ]
        }
        """;

        HttpRequestMessage? capturedRequest = null;

        var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            responseContent,
            request => capturedRequest = request);

        var client = new GarageConfigurationClient(httpClient);

        var result = await client.GetConfigurationAsync();

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("/garage", capturedRequest.RequestUri!.AbsolutePath);

        Assert.Single(result.Sectors);
        Assert.Single(result.Spots);

        var sector = result.Sectors.Single();
        Assert.Equal("A", sector.Sector);
        Assert.Equal(10.0m, sector.BasePrice);
        Assert.Equal(100, sector.MaxCapacity);

        var spot = result.Spots.Single();
        Assert.Equal(1, spot.Id);
        Assert.Equal("A", spot.Sector);
        Assert.Equal(-23.561684m, spot.Latitude);
        Assert.Equal(-46.655981m, spot.Longitude);
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldThrowInvalidOperationException_WhenResponseStatusIsNotSuccessful()
    {
        var httpClient = CreateHttpClient(HttpStatusCode.BadGateway, "upstream failure");
        var client = new GarageConfigurationClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetConfigurationAsync());

        Assert.Contains("Failed to fetch garage configuration.", exception.Message);
        Assert.Contains("502", exception.Message);
        Assert.Contains("BadGateway", exception.Message);
        Assert.Contains("upstream failure", exception.Message);
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldThrowInvalidOperationException_WhenResponseCannotBeDeserialized()
    {
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "null");
        var client = new GarageConfigurationClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetConfigurationAsync());

        Assert.Equal("Garage configuration response could not be deserialized.", exception.Message);
    }

    private static HttpClient CreateHttpClient(
        HttpStatusCode statusCode,
        string content,
        Action<HttpRequestMessage>? onRequest = null)
    {
        var handler = new FakeHttpMessageHandler(request => {
            onRequest?.Invoke(request);

            return new HttpResponseMessage(statusCode) {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
        });

        return new HttpClient(handler) {
            BaseAddress = new Uri("https://localhost:5001/")
        };
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}