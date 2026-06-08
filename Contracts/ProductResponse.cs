namespace ProductCatalogService.Contracts;

public sealed record ProductResponse(
    Guid SkuId,
    Guid SellerId,
    string Title,
    string Category,
    decimal Price,
    string Status,
    decimal WeightKg,
    decimal HeightCm,
    decimal WidthCm,
    decimal LengthCm,
    bool IsFragile,
    bool IsRestricted);
