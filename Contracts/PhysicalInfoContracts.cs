namespace ProductCatalogService.Contracts;

public sealed record BatchPhysicalInfoRequest(
    IReadOnlyList<Guid> SkuIds);

public sealed record ProductPhysicalInfoResponse(
    Guid SkuId,
    Guid SellerId,
    decimal WeightKg,
    decimal HeightCm,
    decimal WidthCm,
    decimal LengthCm,
    string Category,
    bool IsFragile,
    bool IsRestricted,
    string Status);

public sealed record UpdatePhysicalInfoRequest(
    decimal WeightKg,
    ProductDimensionsDto Dimensions,
    bool IsFragile,
    bool IsRestricted);

public sealed record ChangeProductStatusRequest(
    string Status);
