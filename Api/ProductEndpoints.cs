using ProductCatalogService.Application;
using ProductCatalogService.Contracts;

namespace ProductCatalogService.Api;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Products");

        group.MapPost("/", async (
            CreateProductRequest request,
            ProductApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.CreateAsync(request, cancellationToken);

            return Results.Created($"/products/{response.SkuId}", response);
        })
        .WithName("CreateProduct")
        .WithSummary("Creates a product/SKU in the catalog.");

        group.MapGet("/{skuId:guid}", async (
            Guid skuId,
            ProductApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetBySkuIdAsync(skuId, cancellationToken);

            return response is null
                ? Results.NotFound()
                : Results.Ok(response);
        })
        .WithName("GetProductBySku")
        .WithSummary("Gets the complete catalog data for a SKU.");

        group.MapPost("/physical-info/batch", async (
            BatchPhysicalInfoRequest request,
            ProductPhysicalInfoApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetBatchAsync(request, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("GetProductPhysicalInfoBatch")
        .WithSummary("Gets physical information for many active SKUs in one request.");

        group.MapPut("/{skuId:guid}/physical-info", async (
            Guid skuId,
            UpdatePhysicalInfoRequest request,
            ProductApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.UpdatePhysicalInfoAsync(skuId, request, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("UpdateProductPhysicalInfo")
        .WithSummary("Updates physical SKU data and invalidates the physical-info cache.");

        group.MapPatch("/{skuId:guid}/status", async (
            Guid skuId,
            ChangeProductStatusRequest request,
            ProductApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.ChangeStatusAsync(skuId, request, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("ChangeProductStatus")
        .WithSummary("Changes the status of a product/SKU.");

        return app;
    }
}
