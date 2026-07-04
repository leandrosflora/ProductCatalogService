using ProductCatalogService.Api;
using ProductCatalogService.Application;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Infrastructure.Cache;
using ProductCatalogService.Infrastructure.Outbox;
using ProductCatalogService.Infrastructure.Persistence;
using Meli.Observability;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Environment.ApplicationName;
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:5107";

builder.Logging.AddMeliStructuredLogging(serviceName, otlpEndpoint);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options => options.Filter = httpContext =>
            !httpContext.Request.Path.StartsWithSegments("/metrics") &&
            !httpContext.Request.Path.StartsWithSegments("/health"))
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

builder.Services.AddSingleton<ProductCatalogDbConnectionFactory>();
builder.Services.AddScoped<ProductCatalogUnitOfWork>();

builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddScoped<ProductApplicationService>();
builder.Services.AddScoped<ProductPhysicalInfoApplicationService>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductPhysicalInfoCache, RedisProductPhysicalInfoCache>();
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();

builder.Services.AddHealthChecks()
    .AddCheck<ProductCatalogDbHealthCheck>("product-catalog-db");

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
