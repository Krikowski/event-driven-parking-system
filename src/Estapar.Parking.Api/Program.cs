using Estapar.Parking.Infrastructure.DependencyInjection;
using Estapar.Parking.Application.UseCases.Garage;
using Estapar.Parking.Api.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ISyncGarageConfigurationUseCase, SyncGarageConfigurationUseCase>();

builder.Services.AddHostedService<GarageBootstrapHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();