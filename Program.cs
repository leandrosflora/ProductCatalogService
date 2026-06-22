using ProductCatalogService.Api;
using ProductCatalogService.Application;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Infrastructure.Mocking;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Environment.ApplicationName;
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:5107";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ProductApplicationService>();
builder.Services.AddScoped<ProductPhysicalInfoApplicationService>();

builder.Services.AddSingleton<MockProductCatalogStore>();
builder.Services.AddScoped<IProductRepository, MockProductRepository>();
builder.Services.AddSingleton<IProductPhysicalInfoCache, MockProductPhysicalInfoCache>();
builder.Services.AddSingleton<IEventPublisher, MockEventPublisher>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint("/metrics");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");
app.MapProductEndpoints();

app.Run();
