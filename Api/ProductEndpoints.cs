using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.Application;

namespace ProductCatalogService.Api;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/products")
            .WithTags("Products");

        group.MapGet("/logistics/batch", async (
            [FromQuery] Guid[] skuIds,
            ProductPhysicalInfoApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetBatchAsync(skuIds, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("GetProductsLogisticsBatch")
        .WithSummary("Gets logistics information for many active SKUs in one request.");

        group.MapGet("/{skuId:guid}/logistics", async (
            Guid skuId,
            ProductPhysicalInfoApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetBySkuIdAsync(skuId, cancellationToken);

            return response is null
                ? Results.NotFound()
                : Results.Ok(response);
        })
        .WithName("GetProductLogisticsBySku")
        .WithSummary("Gets logistics information for an active SKU.");

        return app;
    }
}
