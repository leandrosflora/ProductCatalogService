using ProductCatalogService.Api;
using ProductCatalogService.Application;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Infrastructure.Mocking;

var builder = WebApplication.CreateBuilder(args);

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");
app.MapProductEndpoints();

app.Run();
