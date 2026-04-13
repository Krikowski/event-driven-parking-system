using System.Text.Json;
using Estapar.Parking.Domain.Exceptions;

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
        } catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception occurred.");

            await WriteResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                ex.Message);
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");

            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteResponseAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = new {
            error = message,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);

        await context.Response.WriteAsync(json);
    }
}