using ProductCatalogService.Application;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Domain;
using ProductCatalogService.Infrastructure.Mocking;
using Xunit;

namespace ProductCatalogService.Tests;

public class ProductLogisticsTitleStatusTests
{
    private sealed class SingleProductRepository : IProductRepository
    {
        private readonly Product _product;

        public SingleProductRepository(Product product) => _product = product;

        public Task<Product?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken) =>
            Task.FromResult(skuId == _product.SkuId ? _product : null);

        public Task<IReadOnlyList<Product>> GetBySkuIdsAsync(
            IReadOnlyCollection<Guid> skuIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Product> result = skuIds.Contains(_product.SkuId) ? [_product] : [];
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<Product> all = [_product];
            return Task.FromResult(all);
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public async Task GetBySkuIdAsync_ForActiveProduct_ReturnsRealTitleAndStatus()
    {
        var product = new Product(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tenis Esportivo Demo",
            "fashion",
            349.90m,
            new ProductDimensions(12m, 20m, 30m),
            0.9m,
            isFragile: false,
            isRestricted: false);

        var service = new ProductPhysicalInfoApplicationService(
            new SingleProductRepository(product),
            new MockProductPhysicalInfoCache());

        var result = await service.GetBySkuIdAsync(product.SkuId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Tenis Esportivo Demo", result!.Title);
        Assert.Equal("Active", result.Status);
    }
}
