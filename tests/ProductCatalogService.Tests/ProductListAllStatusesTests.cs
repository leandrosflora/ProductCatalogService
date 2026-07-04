using ProductCatalogService.Application;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Domain;
using ProductCatalogService.Infrastructure.Mocking;
using Xunit;

namespace ProductCatalogService.Tests;

public class ProductListAllStatusesTests
{
    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products;

        public InMemoryProductRepository(IEnumerable<Product> products) => _products = products.ToList();

        public Task<Product?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken) =>
            Task.FromResult(_products.FirstOrDefault(p => p.SkuId == skuId));

        public Task<IReadOnlyList<Product>> GetBySkuIdsAsync(
            IReadOnlyCollection<Guid> skuIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Product> matches = _products.Where(p => skuIds.Contains(p.SkuId)).ToArray();
            return Task.FromResult(matches);
        }

        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<Product> all = _products;
            return Task.FromResult(all);
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static Product BuildProduct(string title, ProductStatus status)
    {
        var product = new Product(
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            "electronics",
            10m,
            new ProductDimensions(10m, 10m, 10m),
            1m,
            isFragile: false,
            isRestricted: false);

        product.ChangeStatus(status);

        return product;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProductsOfEveryStatus()
    {
        var active = BuildProduct("Ativo", ProductStatus.Active);
        var paused = BuildProduct("Pausado", ProductStatus.Paused);
        var blocked = BuildProduct("Bloqueado", ProductStatus.Blocked);

        var service = new ProductApplicationService(
            new InMemoryProductRepository([active, paused, blocked]),
            new MockProductPhysicalInfoCache(),
            new MockEventPublisher());

        var all = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(3, all.Count);
        Assert.Contains(all, p => p.SkuId == active.SkuId && p.Status == "Active");
        Assert.Contains(all, p => p.SkuId == paused.SkuId && p.Status == "Paused");
        Assert.Contains(all, p => p.SkuId == blocked.SkuId && p.Status == "Blocked");
    }
}
