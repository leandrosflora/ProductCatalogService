using ProductCatalogService.Domain;

namespace ProductCatalogService.Infrastructure.Persistence;

internal sealed class ProductRecord
{
    public Guid Id { get; init; }
    public Guid SellerId { get; init; }
    public Guid SkuId { get; init; }
    public string Title { get; init; } = default!;
    public string Category { get; init; } = default!;
    public decimal Price { get; init; }
    public string Status { get; init; } = default!;
    public decimal WeightKg { get; init; }
    public decimal HeightCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal LengthCm { get; init; }
    public bool IsFragile { get; init; }
    public bool IsRestricted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public Product ToDomain()
    {
        return new Product(
            Id,
            SellerId,
            SkuId,
            Title,
            Category,
            Price,
            Enum.Parse<ProductStatus>(Status, ignoreCase: true),
            new ProductDimensions(HeightCm, WidthCm, LengthCm),
            WeightKg,
            IsFragile,
            IsRestricted,
            CreatedAt,
            UpdatedAt);
    }
}
