using ProductCatalogService.Application;
using ProductCatalogService.Contracts;
using ProductCatalogService.Domain;

namespace ProductCatalogService.Tests.Application;

public sealed class ProductPhysicalInfoApplicationServiceTests
{
    [Fact]
    public async Task GetBatchAsync_WhenRequestIsEmpty_ReturnsEmptyWithoutUsingDependencies()
    {
        var repository = new InMemoryProductRepository();
        var cache = new RecordingProductPhysicalInfoCache();
        var service = new ProductPhysicalInfoApplicationService(repository, cache);

        var response = await service.GetBatchAsync(new BatchPhysicalInfoRequest([]), CancellationToken.None);

        Assert.Empty(response);
        Assert.Equal(0, cache.GetCalls);
        Assert.Equal(0, cache.SetCalls);
    }

    [Fact]
    public async Task GetBatchAsync_WhenSkuIsCached_ReturnsCachedPhysicalInfoWithoutRepositoryLookup()
    {
        var skuId = Guid.NewGuid();
        var cached = new ProductPhysicalInfoResponse(skuId, Guid.NewGuid(), 1m, 2m, 3m, 4m, "cached", false, false, "Active");
        var repository = new InMemoryProductRepository();
        var cache = new RecordingProductPhysicalInfoCache();
        cache.Seed(cached);
        var service = new ProductPhysicalInfoApplicationService(repository, cache);

        var response = await service.GetBatchAsync(new BatchPhysicalInfoRequest([skuId]), CancellationToken.None);

        Assert.Equal([cached], response);
        Assert.Equal(1, cache.GetCalls);
        Assert.Equal(0, cache.SetCalls);
    }

    [Fact]
    public async Task GetBatchAsync_WhenSkuIsMissingFromCache_LoadsActiveProductAndCachesForSixHours()
    {
        var skuId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var repository = new InMemoryProductRepository();
        repository.Seed(new Product(sellerId, skuId, "Product", "category", 100m, new ProductDimensions(10m, 20m, 30m), 2m, true, false));
        var cache = new RecordingProductPhysicalInfoCache();
        var service = new ProductPhysicalInfoApplicationService(repository, cache);

        var response = await service.GetBatchAsync(new BatchPhysicalInfoRequest([skuId, skuId]), CancellationToken.None);

        var item = Assert.Single(response);
        Assert.Equal(skuId, item.SkuId);
        Assert.Equal(sellerId, item.SellerId);
        Assert.Equal(2m, item.WeightKg);
        Assert.Equal("category", item.Category);
        Assert.True(item.IsFragile);
        Assert.False(item.IsRestricted);
        Assert.Equal("Active", item.Status);
        var cacheEntry = Assert.Single(cache.SetEntries);
        Assert.Equal(skuId, cacheEntry.Product.SkuId);
        Assert.Equal(TimeSpan.FromHours(6), cacheEntry.Ttl);
    }

    [Fact]
    public async Task GetBatchAsync_WhenProductIsNotActive_DoesNotReturnOrCacheIt()
    {
        var skuId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), skuId, "Product", "category", 100m, new ProductDimensions(10m, 20m, 30m), 2m, false, false);
        product.ChangeStatus(ProductStatus.Blocked);
        var repository = new InMemoryProductRepository();
        repository.Seed(product);
        var cache = new RecordingProductPhysicalInfoCache();
        var service = new ProductPhysicalInfoApplicationService(repository, cache);

        var response = await service.GetBatchAsync(new BatchPhysicalInfoRequest([skuId]), CancellationToken.None);

        Assert.Empty(response);
        Assert.Empty(cache.SetEntries);
    }
}
