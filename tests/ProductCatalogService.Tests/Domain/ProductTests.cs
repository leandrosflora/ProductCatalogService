using ProductCatalogService.Domain;

namespace ProductCatalogService.Tests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Constructor_WhenValidInput_NormalizesTextAndStartsActive()
    {
        var sellerId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        var product = new Product(
            sellerId,
            skuId,
            "  Smartphone  ",
            "  electronics  ",
            1999.90m,
            new ProductDimensions(10m, 20m, 30m),
            1.5m,
            isFragile: true,
            isRestricted: false);

        Assert.Equal(sellerId, product.SellerId);
        Assert.Equal(skuId, product.SkuId);
        Assert.Equal("Smartphone", product.Title);
        Assert.Equal("electronics", product.Category);
        Assert.Equal(ProductStatus.Active, product.Status);
        Assert.True(product.IsFragile);
        Assert.False(product.IsRestricted);
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.True(product.CreatedAt <= product.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenWeightIsNotPositive_Throws(decimal weightKg)
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateProduct(weightKg: weightKg));

        Assert.Equal("weightKg", exception.ParamName);
    }

    [Fact]
    public void UpdatePhysicalInfo_WhenValidInput_UpdatesPhysicalFieldsAndTimestamp()
    {
        var product = CreateProduct();
        var previousUpdatedAt = product.UpdatedAt;

        product.UpdatePhysicalInfo(2.5m, new ProductDimensions(11m, 22m, 33m), isFragile: false, isRestricted: true);

        Assert.Equal(2.5m, product.WeightKg);
        Assert.Equal(11m, product.Dimensions.HeightCm);
        Assert.Equal(22m, product.Dimensions.WidthCm);
        Assert.Equal(33m, product.Dimensions.LengthCm);
        Assert.False(product.IsFragile);
        Assert.True(product.IsRestricted);
        Assert.True(product.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void ChangeStatus_WhenCalled_UpdatesStatusAndTimestamp()
    {
        var product = CreateProduct();
        var previousUpdatedAt = product.UpdatedAt;

        product.ChangeStatus(ProductStatus.Paused);

        Assert.Equal(ProductStatus.Paused, product.Status);
        Assert.True(product.UpdatedAt >= previousUpdatedAt);
    }

    private static Product CreateProduct(decimal weightKg = 1m)
    {
        return new Product(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "category",
            10m,
            new ProductDimensions(1m, 2m, 3m),
            weightKg,
            isFragile: false,
            isRestricted: false);
    }
}
