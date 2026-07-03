using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.Application;
using ProductCatalogService.Contracts;

namespace ProductCatalogService.Api;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/products")
            .WithTags("Products");

        group.MapPost("/", async (
            CreateProductRequest request,
            ProductApplicationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await service.CreateAsync(request, cancellationToken);
                return Results.Created($"/v1/products/{response.SkuId}", response);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [exception.ParamName ?? "request"] = [exception.Message]
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.Conflict(new ProblemDetails { Title = exception.Message });
            }
        })
        .WithName("CreateProduct")
        .WithSummary("Creates a product (commercial + logistics attributes).");

        group.MapGet("/{skuId:guid}", async (
            Guid skuId,
            ProductApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetBySkuIdAsync(skuId, cancellationToken);

            return response is null ? Results.NotFound() : Results.Ok(response);
        })
        .WithName("GetProduct")
        .WithSummary("Gets a product by SKU id.");

        group.MapPut("/{skuId:guid}/logistics", async (
            Guid skuId,
            UpdatePhysicalInfoRequest request,
            ProductApplicationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await service.UpdatePhysicalInfoAsync(skuId, request, cancellationToken);
                return Results.Ok(response);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [exception.ParamName ?? "request"] = [exception.Message]
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.NotFound(new ProblemDetails { Title = exception.Message });
            }
        })
        .WithName("UpdateProductLogistics")
        .WithSummary("Updates a product's logistics attributes (weight, dimensions, fragile/restricted flags).");

        group.MapPatch("/{skuId:guid}/status", async (
            Guid skuId,
            ChangeProductStatusRequest request,
            ProductApplicationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await service.ChangeStatusAsync(skuId, request, cancellationToken);
                return Results.Ok(response);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = [exception.Message]
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.NotFound(new ProblemDetails { Title = exception.Message });
            }
        })
        .WithName("ChangeProductStatus")
        .WithSummary("Activates, pauses, blocks, or otherwise changes a product's status.");

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
