using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

using Estapar.Parking.Api.Models.Responses;
using Estapar.Parking.Domain.Entities;
using Estapar.Parking.Domain.Enums;
using Estapar.Parking.IntegrationTests.Infrastructure;

using Microsoft.EntityFrameworkCore;

namespace Estapar.Parking.IntegrationTests;

public sealed class WebhookIntegrationTests
{

    [Fact]
    public async Task Post_Webhook_ShouldReturnStructuredValidationError_WhenPayloadIsInvalid()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PostAsync("/webhook", CreateJsonContent(new
        {
            event_type = "ENTRY",
            license_plate = "ZUL0001"
        }));

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponseModel>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("invalid_request", payload!.Code);
        Assert.Contains("Entry time is required for ENTRY events.", payload.Details!);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task Post_Webhook_ShouldIgnoreDuplicateEntry()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageAsync(factory);

        using var client = factory.CreateClient();

        var payload = new
        {
            event_type = "ENTRY",
            license_plate = "ZUL0001",
            entry_time = "2025-01-01T12:00:00Z"
        };

        var firstResponse = await client.PostAsync("/webhook", CreateJsonContent(payload));
        var secondResponse = await client.PostAsync("/webhook", CreateJsonContent(payload));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var sessionsCount = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<ParkingSession>().CountAsync());

        var eventsCount = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<VehicleEvent>().CountAsync(e => e.EventType == ParkingEventType.Entry));

        var parkingSpot = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<ParkingSpot>().SingleAsync());

        Assert.Equal(1, sessionsCount);
        Assert.Equal(1, eventsCount);
        Assert.True(parkingSpot.IsOccupied);
    }

    [Fact]
    public async Task Post_Webhook_ShouldIgnoreDuplicateParked()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageAsync(factory);

        using var client = factory.CreateClient();

        await client.PostAsync("/webhook", CreateJsonContent(new
        {
            event_type = "ENTRY",
            license_plate = "ZUL0001",
            entry_time = "2025-01-01T12:00:00Z"
        }));

        var payload = new
        {
            event_type = "PARKED",
            license_plate = "ZUL0001",
            lat = -23.561684m,
            lng = -46.655981m
        };

        var firstResponse = await client.PostAsync("/webhook", CreateJsonContent(payload));
        var secondResponse = await client.PostAsync("/webhook", CreateJsonContent(payload));

        var session = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<ParkingSession>().SingleAsync());

        var spot = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<ParkingSpot>().SingleAsync());

        var count = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<VehicleEvent>().CountAsync(e => e.EventType == ParkingEventType.Parked));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(spot.Id, session.ParkingSpotId);
        Assert.True(spot.IsOccupied);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Post_Webhook_ShouldRejectParked_WhenCoordinatesDoNotMatchReservedSpot()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageWithTwoSpotsAsync(factory);

        using var client = factory.CreateClient();

        var entryResponse = await client.PostAsync("/webhook", CreateJsonContent(new
        {
            event_type = "ENTRY",
            license_plate = "ZUL0001",
            entry_time = "2025-01-01T12:00:00Z"
        }));

        var parkedResponse = await client.PostAsync("/webhook", CreateJsonContent(new
        {
            event_type = "PARKED",
            license_plate = "ZUL0001",
            lat = -23.561685m,
            lng = -46.655982m
        }));

        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, parkedResponse.StatusCode);
    }

    [Fact]
    public async Task Post_Webhook_ShouldRejectEntry_WhenNoPhysicalSpotIsAvailable()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await factory.ExecuteDbContextAsync<object>(async db =>
        {
            db.Set<Sector>().Add(new Sector("A", 2, 100m));
            db.Set<ParkingSpot>().Add(new ParkingSpot(1, "A", -23.561684m, -46.655981m));
            await db.SaveChangesAsync();
            return null!;
        });

        using var client = factory.CreateClient();

        var firstResponse = await client.PostAsync("/webhook", CreateJsonContent(new
        {
            event_type = "ENTRY",
            license_plate = "ZUL0001",
            entry_time = "2025-01-01T12:00:00Z"
        }));

        var secondResponse = await client.PostAsync("/webhook", CreateJsonContent(new
        {
            event_type = "ENTRY",
            license_plate = "ZUL0002",
            entry_time = "2025-01-01T12:05:00Z"
        }));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Post_Webhook_ShouldIgnoreDuplicateExit()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        await SeedGarageAsync(factory);

        using var client = factory.CreateClient();

        await client.PostAsync("/webhook", CreateJsonContent(new
        {
            event_type = "ENTRY",
            license_plate = "ZUL0001",
            entry_time = "2025-01-01T12:00:00Z"
        }));

        await client.PostAsync("/webhook", CreateJsonContent(new
        {
            event_type = "PARKED",
            license_plate = "ZUL0001",
            lat = -23.561684m,
            lng = -46.655981m
        }));

        var payload = new
        {
            event_type = "EXIT",
            license_plate = "ZUL0001",
            exit_time = "2025-01-01T13:31:00Z"
        };

        var firstResponse = await client.PostAsync("/webhook", CreateJsonContent(payload));
        var secondResponse = await client.PostAsync("/webhook", CreateJsonContent(payload));

        var session = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<ParkingSession>().SingleAsync());

        var spot = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<ParkingSpot>().SingleAsync());

        var count = await factory.ExecuteDbContextAsync(async db =>
            await db.Set<VehicleEvent>().CountAsync(e => e.EventType == ParkingEventType.Exit));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(ParkingSessionStatus.Closed, session.Status);
        Assert.False(spot.IsOccupied);
        Assert.Equal(1, count);
    }

    private static StringContent CreateJsonContent(object payload)
    {
        return new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
    }

    private static async Task SeedGarageAsync(CustomWebApplicationFactory factory)
    {
        await factory.ExecuteDbContextAsync<object>(async db =>
        {
            db.Set<Sector>().Add(new Sector("A", 10, 100m));
            db.Set<ParkingSpot>().Add(new ParkingSpot(1, "A", -23.561684m, -46.655981m));

            await db.SaveChangesAsync();
            return null!;
        });
    }

    private static async Task SeedGarageWithTwoSpotsAsync(CustomWebApplicationFactory factory)
    {
        await factory.ExecuteDbContextAsync<object>(async db =>
        {
            db.Set<Sector>().Add(new Sector("A", 10, 100m));
            db.Set<ParkingSpot>().Add(new ParkingSpot(1, "A", -23.561684m, -46.655981m));
            db.Set<ParkingSpot>().Add(new ParkingSpot(2, "A", -23.561685m, -46.655982m));

            await db.SaveChangesAsync();
            return null!;
        });
    }
}
