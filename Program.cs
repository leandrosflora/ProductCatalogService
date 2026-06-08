using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Api;
using ProductCatalogService.Application;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Infrastructure.Cache;
using ProductCatalogService.Infrastructure.Outbox;
using ProductCatalogService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ProductCatalogDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProductCatalogDb"));
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "product-catalog:";
});

builder.Services.AddScoped<ProductApplicationService>();
builder.Services.AddScoped<ProductPhysicalInfoApplicationService>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductPhysicalInfoCache, RedisProductPhysicalInfoCache>();
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductCatalogDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");
app.MapProductEndpoints();

app.Run();
