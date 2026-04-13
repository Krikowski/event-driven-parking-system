using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.Infrastructure.Persistence;
using Estapar.Parking.IntegrationTests.Infrastructure;

using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.IntegrationTests;

public sealed class WebhookIntegrationTests
{
    [Fact]
    public async Task Post_Webhook_ShouldProcessEntrySuccessfully()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/webhook",
            CreateJsonContent(new
            {
                event_type = "ENTRY",
                license_plate = "ZUL0001",
                entry_time = "2025-01-01T12:00:00Z"
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Set<ParkingSession>().SingleOrDefaultAsync());

        Assert.NotNull(session);
        Assert.Equal("ZUL0001", session!.LicensePlate);
        Assert.Equal("A", session.SectorCode);
        Assert.Equal(ParkingSessionStatus.Active, session.Status);

        var events = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Set<VehicleEvent>().ToListAsync());

        Assert.Single(events);
        Assert.Equal(ParkingEventType.Entry, events[0].EventType);
    }

    [Fact]
    public async Task Post_Webhook_ShouldProcessParkedSuccessfully()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageAsync(factory);

        using var client = factory.CreateClient();

        await client.PostAsync(
            "/webhook",
            CreateJsonContent(new
            {
                event_type = "ENTRY",
                license_plate = "ZUL0001",
                entry_time = "2025-01-01T12:00:00Z"
            }));

        var response = await client.PostAsync(
            "/webhook",
            CreateJsonContent(new
            {
                event_type = "PARKED",
                license_plate = "ZUL0001",
                lat = -23.561684m,
                lng = -46.655981m
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Set<ParkingSession>().SingleAsync());

        var spot = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Set<ParkingSpot>().SingleAsync());

        Assert.Equal(spot.Id, session.ParkingSpotId);
        Assert.True(spot.IsOccupied);

        var events = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Set<VehicleEvent>().OrderBy(x => x.EventType).ToListAsync());

        Assert.Equal(2, events.Count);
        Assert.Contains(events, x => x.EventType == ParkingEventType.Parked);
    }

    [Fact]
    public async Task Post_Webhook_ShouldProcessExitSuccessfully()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageAsync(factory);

        using var client = factory.CreateClient();

        await client.PostAsync(
            "/webhook",
            CreateJsonContent(new
            {
                event_type = "ENTRY",
                license_plate = "ZUL0001",
                entry_time = "2025-01-01T12:00:00Z"
            }));

        await client.PostAsync(
            "/webhook",
            CreateJsonContent(new
            {
                event_type = "PARKED",
                license_plate = "ZUL0001",
                lat = -23.561684m,
                lng = -46.655981m
            }));

        var response = await client.PostAsync(
            "/webhook",
            CreateJsonContent(new
            {
                event_type = "EXIT",
                license_plate = "ZUL0001",
                exit_time = "2025-01-01T13:31:00Z"
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Set<ParkingSession>().SingleAsync());

        var spot = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Set<ParkingSpot>().SingleAsync());

        Assert.Equal(ParkingSessionStatus.Closed, session.Status);
        Assert.NotNull(session.ExitTimeUtc);
        Assert.NotNull(session.ChargedAmount);
        Assert.False(spot.IsOccupied);

        var events = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.Set<VehicleEvent>().ToListAsync());

        Assert.Equal(3, events.Count);
        Assert.Contains(events, x => x.EventType == ParkingEventType.Exit);
    }

    [Fact]
    public async Task Post_Webhook_ShouldReturnBadRequest_WhenEventTypeIsInvalid()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/webhook",
            CreateJsonContent(new
            {
                event_type = "UNKNOWN",
                license_plate = "ZUL0001"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unsupported event type.", body);
    }

    [Fact]
    public async Task Post_Webhook_ShouldReturnBadRequest_WhenExitHasNoActiveSession()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/webhook",
            CreateJsonContent(new
            {
                event_type = "EXIT",
                license_plate = "ZUL0001",
                exit_time = "2025-01-01T13:31:00Z"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No active parking session was found for this license plate.", body);
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

    private static StringContent CreateJsonContent(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}