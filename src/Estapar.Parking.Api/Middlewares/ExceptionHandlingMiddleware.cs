using System.Text.Json;

using Estapar.Parking.Api.Models.Responses;
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
            _logger.LogWarning(
                ex,
                "Domain exception occurred. TraceId: {TraceId}",
                context.TraceIdentifier);

            await WriteResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                "domain_error",
                ex.Message);
        } catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred. TraceId: {TraceId}",
                context.TraceIdentifier);

            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "unexpected_error",
                ex.Message);
        }
    }

    private static async Task WriteResponseAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = new ErrorResponseModel
        {
            Code = code,
            Message = message,
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);

        await context.Response.WriteAsync(json);
    }
}