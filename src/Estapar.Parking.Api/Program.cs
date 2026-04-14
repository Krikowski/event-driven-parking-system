using Estapar.Parking.Api.HostedServices;
using Estapar.Parking.Api.Middlewares;
using Estapar.Parking.Api.Models.Responses;
using Estapar.Parking.Application.UseCases.Entry;
using Estapar.Parking.Application.UseCases.Exit;
using Estapar.Parking.Application.UseCases.Garage;
using Estapar.Parking.Application.UseCases.Parked;
using Estapar.Parking.Application.UseCases.Revenue;
using Estapar.Parking.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ISyncGarageConfigurationUseCase, SyncGarageConfigurationUseCase>();

builder.Services.AddHostedService<GarageBootstrapHostedService>();

builder.Services.AddScoped<IHandleEntryEventUseCase, HandleEntryEventUseCase>();
builder.Services.AddScoped<IHandleParkedEventUseCase, HandleParkedEventUseCase>();
builder.Services.AddScoped<IHandleExitEventUseCase, HandleExitEventUseCase>();
builder.Services.AddScoped<IGetRevenueUseCase, GetRevenueUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new HealthResponseModel
{
    Status = "healthy",
    Service = "estapar-parking-api",
    Timestamp = DateTime.UtcNow
}));

app.Run();

public partial class Program
{
}
