using System.Net;
using System.Net.Http.Json;

using Estapar.Parking.Api.Models.Responses;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.IntegrationTests.Infrastructure;

namespace Estapar.Parking.IntegrationTests;

public sealed class RevenueIntegrationTests
{

    [Fact]
    public async Task Get_Revenue_ShouldReturnStructuredValidationError_WhenQueryIsInvalid()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/revenue");
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponseModel>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("invalid_request", payload!.Code);
        Assert.Contains("Sector is required.", payload.Details!);
        Assert.Contains("Date is required.", payload.Details!);
    }

    [Fact]
    public async Task Get_Revenue_ShouldReturnCorrectAmount_AfterCompleteWebhookFlow()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageAsync(factory);

        using var client = factory.CreateClient();

        await PostEntryAsync(client, "ZUL0001", "2025-01-01T12:00:00Z");
        await PostParkedAsync(client, "ZUL0001", -23.561684m, -46.655981m);
        await PostExitAsync(client, "ZUL0001", "2025-01-01T13:31:00Z");

        var response = await client.GetAsync("/revenue?sector=A&date=2025-01-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var revenue = await response.Content.ReadFromJsonAsync<RevenueResponseModel>();

        Assert.NotNull(revenue);
        Assert.Equal(18.00m, revenue!.Amount);
        Assert.Equal("BRL", revenue.Currency);
        Assert.True(revenue.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public async Task Get_Revenue_ShouldFilterBySectorCorrectly()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await factory.SeedAsync(async dbContext =>
        {
            dbContext.Set<Sector>().AddRange(
                new Sector("A", 10, 10m),
                new Sector("B", 10, 20m));

            dbContext.Set<ParkingSession>().AddRange(
                CreateClosedSession(
                    licensePlate: "ZULA001",
                    sectorCode: "A",
                    entryTimeUtc: CreateUtcDate(2025, 1, 1, 12, 0, 0),
                    frozenHourlyRate: 10m,
                    exitTimeUtc: CreateUtcDate(2025, 1, 1, 13, 31, 0),
                    chargedAmount: 18m),
                CreateClosedSession(
                    licensePlate: "ZULB001",
                    sectorCode: "B",
                    entryTimeUtc: CreateUtcDate(2025, 1, 1, 12, 0, 0),
                    frozenHourlyRate: 20m,
                    exitTimeUtc: CreateUtcDate(2025, 1, 1, 13, 31, 0),
                    chargedAmount: 36m));

            await Task.CompletedTask;
        });

        using var client = factory.CreateClient();

        var responseA = await client.GetAsync("/revenue?sector=A&date=2025-01-01");
        var responseB = await client.GetAsync("/revenue?sector=B&date=2025-01-01");

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);

        var revenueA = await responseA.Content.ReadFromJsonAsync<RevenueResponseModel>();
        var revenueB = await responseB.Content.ReadFromJsonAsync<RevenueResponseModel>();

        Assert.NotNull(revenueA);
        Assert.NotNull(revenueB);

        Assert.Equal(18.00m, revenueA!.Amount);
        Assert.Equal("BRL", revenueA.Currency);

        Assert.Equal(36.00m, revenueB!.Amount);
        Assert.Equal("BRL", revenueB.Currency);
    }

    [Fact]
    public async Task Get_Revenue_ShouldFilterByDateCorrectly()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await factory.SeedAsync(async dbContext =>
        {
            dbContext.Set<Sector>().Add(new Sector("A", 10, 10m));

            dbContext.Set<ParkingSession>().AddRange(
                CreateClosedSession(
                    licensePlate: "ZUL0001",
                    sectorCode: "A",
                    entryTimeUtc: CreateUtcDate(2025, 1, 1, 12, 0, 0),
                    frozenHourlyRate: 10m,
                    exitTimeUtc: CreateUtcDate(2025, 1, 1, 13, 31, 0),
                    chargedAmount: 18m),
                CreateClosedSession(
                    licensePlate: "ZUL0002",
                    sectorCode: "A",
                    entryTimeUtc: CreateUtcDate(2025, 1, 2, 12, 0, 0),
                    frozenHourlyRate: 10m,
                    exitTimeUtc: CreateUtcDate(2025, 1, 2, 13, 31, 0),
                    chargedAmount: 18m));

            await Task.CompletedTask;
        });

        using var client = factory.CreateClient();

        var responseDay1 = await client.GetAsync("/revenue?sector=A&date=2025-01-01");
        var responseDay2 = await client.GetAsync("/revenue?sector=A&date=2025-01-02");

        Assert.Equal(HttpStatusCode.OK, responseDay1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseDay2.StatusCode);

        var revenueDay1 = await responseDay1.Content.ReadFromJsonAsync<RevenueResponseModel>();
        var revenueDay2 = await responseDay2.Content.ReadFromJsonAsync<RevenueResponseModel>();

        Assert.NotNull(revenueDay1);
        Assert.NotNull(revenueDay2);

        Assert.Equal(18.00m, revenueDay1!.Amount);
        Assert.Equal(18.00m, revenueDay2!.Amount);
    }

    private static async Task SeedGarageAsync(CustomWebApplicationFactory factory)
    {
        await factory.SeedAsync(async dbContext =>
        {
            dbContext.Set<Sector>().Add(new Sector("A", 10, 10m));

            dbContext.Set<ParkingSpot>().Add(new ParkingSpot(
                1,
                "A",
                -23.561684m,
                -46.655981m));

            await Task.CompletedTask;
        });
    }

    private static ParkingSession CreateClosedSession(
        string licensePlate,
        string sectorCode,
        DateTime entryTimeUtc,
        decimal frozenHourlyRate,
        DateTime exitTimeUtc,
        decimal chargedAmount)
    {
        var session = new ParkingSession(
            licensePlate,
            sectorCode,
            entryTimeUtc,
            frozenHourlyRate);

        session.Close(exitTimeUtc, chargedAmount);

        return session;
    }

    private static Task<HttpResponseMessage> PostEntryAsync(HttpClient client, string licensePlate, string entryTimeUtc)
    {
        return client.PostAsJsonAsync("/webhook", new
        {
            event_type = "ENTRY",
            license_plate = licensePlate,
            entry_time = entryTimeUtc
        });
    }

    private static Task<HttpResponseMessage> PostParkedAsync(HttpClient client, string licensePlate, decimal lat, decimal lng)
    {
        return client.PostAsJsonAsync("/webhook", new
        {
            event_type = "PARKED",
            license_plate = licensePlate,
            lat,
            lng
        });
    }

    private static Task<HttpResponseMessage> PostExitAsync(HttpClient client, string licensePlate, string exitTimeUtc)
    {
        return client.PostAsJsonAsync("/webhook", new
        {
            event_type = "EXIT",
            license_plate = licensePlate,
            exit_time = exitTimeUtc
        });
    }

    private static DateTime CreateUtcDate(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }
}