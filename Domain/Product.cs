namespace ProductCatalogService.Domain;

public sealed class Product
{
    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid SkuId { get; private set; }

    public string Title { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public decimal Price { get; private set; }

    public ProductStatus Status { get; private set; }

    public decimal WeightKg { get; private set; }
    public ProductDimensions Dimensions { get; private set; } = default!;

    public bool IsFragile { get; private set; }
    public bool IsRestricted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Product()
    {
    }

    internal Product(
        Guid id,
        Guid sellerId,
        Guid skuId,
        string title,
        string category,
        decimal price,
        ProductStatus status,
        ProductDimensions dimensions,
        decimal weightKg,
        bool isFragile,
        bool isRestricted,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        SellerId = sellerId;
        SkuId = skuId;
        Title = title;
        Category = category;
        Price = price;
        Status = status;
        Dimensions = dimensions;
        WeightKg = weightKg;
        IsFragile = isFragile;
        IsRestricted = isRestricted;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Product(
        Guid sellerId,
        Guid skuId,
        string title,
        string category,
        decimal price,
        ProductDimensions dimensions,
        decimal weightKg,
        bool isFragile,
        bool isRestricted)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("SellerId is required", nameof(sellerId));
        }

        if (skuId == Guid.Empty)
        {
            throw new ArgumentException("SkuId is required", nameof(skuId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required", nameof(category));
        }

        if (price < 0)
        {
            throw new ArgumentException("Price cannot be negative", nameof(price));
        }

        if (weightKg <= 0)
        {
            throw new ArgumentException("Weight must be greater than zero", nameof(weightKg));
        }

        Id = Guid.NewGuid();
        SellerId = sellerId;
        SkuId = skuId;
        Title = title.Trim();
        Category = category.Trim();
        Price = price;
        Dimensions = dimensions;
        WeightKg = weightKg;
        IsFragile = isFragile;
        IsRestricted = isRestricted;
        Status = ProductStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePhysicalInfo(
        decimal weightKg,
        ProductDimensions dimensions,
        bool isFragile,
        bool isRestricted)
    {
        if (weightKg <= 0)
        {
            throw new ArgumentException("Weight must be greater than zero", nameof(weightKg));
        }

        WeightKg = weightKg;
        Dimensions = dimensions;
        IsFragile = isFragile;
        IsRestricted = isRestricted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeStatus(ProductStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
