using System.Text.Json;

using Estapar.Parking.Api.Models.Responses;
using Estapar.Parking.Domain.Exceptions;
using Estapar.Parking.Infrastructure.Persistence.Exceptions;

namespace Estapar.Parking.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (PersistenceConflictException ex) when (ex.ConflictType == PersistenceConflictType.DuplicateWebhookEvent)
        {
            _logger.LogInformation(
                ex,
                "Duplicate webhook event ignored. TraceId: {TraceId}",
                context.TraceIdentifier);

            await WriteSuccessAsync(
                context,
                "ignored",
                "Duplicate webhook event received. Previous successful processing was preserved.");
        }
        catch (PersistenceConflictException ex) when (
            ex.ConflictType == PersistenceConflictType.ActiveSessionAlreadyExists ||
            ex.ConflictType == PersistenceConflictType.ParkingSpotAlreadyAssigned ||
            ex.ConflictType == PersistenceConflictType.UnknownUniqueConstraint)
        {
            _logger.LogWarning(
                ex,
                "Persistence conflict occurred. TraceId: {TraceId}",
                context.TraceIdentifier);

            await WriteResponseAsync(
                context,
                StatusCodes.Status409Conflict,
                "persistence_conflict",
                "A conflicting state was detected while processing the request.");
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex,
                "Domain exception occurred. TraceId: {TraceId}",
                context.TraceIdentifier);

            await WriteResponseAsync(
                context,
                StatusCodes.Status422UnprocessableEntity,
                "domain_error",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred. TraceId: {TraceId}",
                context.TraceIdentifier);

            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "unexpected_error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteSuccessAsync(
        HttpContext context,
        string status,
        string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status200OK;

        var payload = new WebhookAcceptedResponseModel
        {
            Status = status,
            Message = message,
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);
        await context.Response.WriteAsync(json);
    }

    private static async Task WriteResponseAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        IReadOnlyCollection<string>? details = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = new ErrorResponseModel
        {
            Code = code,
            Message = message,
            Details = details,
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);

        await context.Response.WriteAsync(json);
    }
}
