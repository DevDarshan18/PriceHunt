using System.Text.Json.Serialization;
using PriceHunt.API.Mapping;
using PriceHunt.API.Middleware;
using PriceHunt.Infrastructure.DI;
using PriceHunt.Infrastructure.Simulation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseSerilog(
    (context, logger) => logger.ReadFrom.Configuration(context.Configuration),
    preserveStaticLogger: true,
    writeToProviders: true);

var databasePath = builder.Configuration["Database:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "pricehunt.db");

builder.Services.AddPriceHuntInfrastructure(databasePath);

builder.Services.AddSingleton<ApiMapper>();
builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection(SimulationOptions.SectionName));

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(RequestLoggingMiddleware.CorrelationIdHeader)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
await app.Services.InitializePriceHuntDatabaseAsync();

app.UsePriceHuntMiddleware();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program;
