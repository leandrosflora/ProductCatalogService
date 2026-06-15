using ProductCatalogService.Application;
using ProductCatalogService.Contracts;
using ProductCatalogService.Domain;

namespace ProductCatalogService.Tests.Application;

public sealed class ProductApplicationServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenSkuDoesNotExist_PersistsProductAndAddsProductCreatedEvent()
    {
        var repository = new InMemoryProductRepository();
        var cache = new RecordingProductPhysicalInfoCache();
        var publisher = new RecordingEventPublisher();
        var service = new ProductApplicationService(repository, cache, publisher);
        var request = CreateRequest();

        var response = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(request.SkuId, response.SkuId);
        Assert.Equal(request.SellerId, response.SellerId);
        Assert.Equal("Active", response.Status);
        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(1, repository.SaveChangesCalls);
        var @event = Assert.Single(publisher.Events);
        Assert.Equal("ProductCreated", @event.EventType);
    }

    [Fact]
    public async Task CreateAsync_WhenSkuAlreadyExists_ThrowsAndDoesNotPublishEvent()
    {
        var repository = new InMemoryProductRepository();
        var request = CreateRequest();
        repository.Seed(CreateProduct(request.SellerId, request.SkuId));
        var publisher = new RecordingEventPublisher();
        var service = new ProductApplicationService(repository, new RecordingProductPhysicalInfoCache(), publisher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));

        Assert.Equal("Product already exists for this SKU", exception.Message);
        Assert.Empty(publisher.Events);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task UpdatePhysicalInfoAsync_WhenProductExists_UpdatesProductInvalidatesCacheAndAddsEvent()
    {
        var repository = new InMemoryProductRepository();
        var skuId = Guid.NewGuid();
        repository.Seed(CreateProduct(Guid.NewGuid(), skuId));
        var cache = new RecordingProductPhysicalInfoCache();
        var publisher = new RecordingEventPublisher();
        var service = new ProductApplicationService(repository, cache, publisher);
        var request = new UpdatePhysicalInfoRequest(3.2m, new ProductDimensionsDto(15m, 25m, 35m), true, true);

        var response = await service.UpdatePhysicalInfoAsync(skuId, request, CancellationToken.None);

        Assert.Equal(3.2m, response.WeightKg);
        Assert.Equal(15m, response.HeightCm);
        Assert.Equal(25m, response.WidthCm);
        Assert.Equal(35m, response.LengthCm);
        Assert.True(response.IsFragile);
        Assert.True(response.IsRestricted);
        Assert.Equal([skuId], cache.RemovedSkuIds);
        Assert.Equal(1, repository.SaveChangesCalls);
        var @event = Assert.Single(publisher.Events);
        Assert.Equal("ProductPhysicalInfoChanged", @event.EventType);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenStatusIsValid_ChangesStatusInvalidatesCacheAndAddsEvent()
    {
        var repository = new InMemoryProductRepository();
        var skuId = Guid.NewGuid();
        repository.Seed(CreateProduct(Guid.NewGuid(), skuId));
        var cache = new RecordingProductPhysicalInfoCache();
        var publisher = new RecordingEventPublisher();
        var service = new ProductApplicationService(repository, cache, publisher);

        var response = await service.ChangeStatusAsync(skuId, new ChangeProductStatusRequest("paused"), CancellationToken.None);

        Assert.Equal("Paused", response.Status);
        Assert.Equal([skuId], cache.RemovedSkuIds);
        Assert.Equal(1, repository.SaveChangesCalls);
        var @event = Assert.Single(publisher.Events);
        Assert.Equal("ProductStatusChanged", @event.EventType);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenStatusIsInvalid_ThrowsAndDoesNotSave()
    {
        var repository = new InMemoryProductRepository();
        var skuId = Guid.NewGuid();
        repository.Seed(CreateProduct(Guid.NewGuid(), skuId));
        var service = new ProductApplicationService(repository, new RecordingProductPhysicalInfoCache(), new RecordingEventPublisher());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ChangeStatusAsync(skuId, new ChangeProductStatusRequest("invalid"), CancellationToken.None));

        Assert.Equal("request", exception.ParamName);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

    private static CreateProductRequest CreateRequest()
    {
        return new CreateProductRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "category",
            100m,
            new ProductDimensionsDto(10m, 20m, 30m),
            1.2m,
            false,
            false);
    }

    private static Product CreateProduct(Guid sellerId, Guid skuId)
    {
        return new Product(sellerId, skuId, "Product", "category", 100m, new ProductDimensions(10m, 20m, 30m), 1.2m, false, false);
    }
}
