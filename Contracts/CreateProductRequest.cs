namespace ProductCatalogService.Contracts;

public sealed record CreateProductRequest(
    Guid SellerId,
    Guid SkuId,
    string Title,
    string Category,
    decimal Price,
    ProductDimensionsDto Dimensions,
    decimal WeightKg,
    bool IsFragile,
    bool IsRestricted,
    string? ImageUrl = null);

public sealed record ProductDimensionsDto(
    decimal HeightCm,
    decimal WidthCm,
    decimal LengthCm);
