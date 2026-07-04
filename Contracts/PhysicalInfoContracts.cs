namespace ProductCatalogService.Contracts;

public sealed record ProductLogisticsResponse(
    Guid SkuId,
    Guid SellerId,
    decimal WeightKg,
    decimal HeightCm,
    decimal WidthCm,
    decimal LengthCm,
    string Category,
    decimal Price,
    IReadOnlyList<string> RestrictionCodes,
    string? ImageUrl,
    string Title,
    string Status);

public sealed record UpdatePhysicalInfoRequest(
    decimal WeightKg,
    ProductDimensionsDto Dimensions,
    bool IsFragile,
    bool IsRestricted);

public sealed record ChangeProductStatusRequest(
    string Status);
