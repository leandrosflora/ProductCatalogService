using ProductCatalogService.Domain;

namespace ProductCatalogService.Tests.Domain;

public sealed class ProductDimensionsTests
{
    [Fact]
    public void VolumeCm3_ReturnsHeightTimesWidthTimesLength()
    {
        var dimensions = new ProductDimensions(2m, 3m, 4m);

        Assert.Equal(24m, dimensions.VolumeCm3);
    }

    [Theory]
    [InlineData(0, 1, 1, "heightCm")]
    [InlineData(1, 0, 1, "widthCm")]
    [InlineData(1, 1, 0, "lengthCm")]
    public void Constructor_WhenAnyDimensionIsNotPositive_Throws(decimal height, decimal width, decimal length, string paramName)
    {
        var exception = Assert.Throws<ArgumentException>(() => new ProductDimensions(height, width, length));

        Assert.Equal(paramName, exception.ParamName);
    }
}
