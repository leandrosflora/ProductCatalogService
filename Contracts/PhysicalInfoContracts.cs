namespace ProductCatalogService.Contracts;

public sealed record ProductLogisticsResponse(
    Guid SkuId,
    Guid SellerId,
    decimal WeightKg,
    decimal HeightCm,
    decimal WidthCm,
    decimal LengthCm,
    string Category,
    IReadOnlyList<string> RestrictionCodes);

public sealed record UpdatePhysicalInfoRequest(
    decimal WeightKg,
    ProductDimensionsDto Dimensions,
    bool IsFragile,
    bool IsRestricted);

public sealed record ChangeProductStatusRequest(
    string Status);
