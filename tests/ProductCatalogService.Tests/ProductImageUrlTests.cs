using ProductCatalogService.Application;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Contracts;
using ProductCatalogService.Domain;
using ProductCatalogService.Infrastructure.Mocking;
using Xunit;

namespace ProductCatalogService.Tests;

public class ProductImageUrlTests
{
    private static ProductApplicationService CreateService()
    {
        return new ProductApplicationService(
            new InMemoryProductRepository(),
            new MockProductPhysicalInfoCache(),
            new MockEventPublisher());
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products = new();

        public Task<Product?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_products.GetValueOrDefault(skuId));
        }

        public Task<IReadOnlyList<Product>> GetBySkuIdsAsync(
            IReadOnlyCollection<Guid> skuIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Product> matches = skuIds
                .Select(skuId => _products.GetValueOrDefault(skuId))
                .Where(product => product is not null)
                .Select(product => product!)
                .ToArray();

            return Task.FromResult(matches);
        }

        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<Product> all = _products.Values.ToArray();
            return Task.FromResult(all);
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken)
        {
            _products[product.SkuId] = product;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CreateAsync_WithImageUrl_RoundTripsUnchanged()
    {
        var service = CreateService();
        var request = new CreateProductRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Produto com imagem",
            "electronics",
            99.90m,
            new ProductDimensionsDto(10m, 10m, 10m),
            1.0m,
            IsFragile: false,
            IsRestricted: false,
            ImageUrl: "https://upload.wikimedia.org/wikipedia/commons/7/7e/Galaxy_J_SC-02F_Lapis_Blue_1.jpg");

        var created = await service.CreateAsync(request, CancellationToken.None);
        var fetched = await service.GetBySkuIdAsync(request.SkuId, CancellationToken.None);

        Assert.Equal(request.ImageUrl, created.ImageUrl);
        Assert.NotNull(fetched);
        Assert.Equal(request.ImageUrl, fetched!.ImageUrl);
    }

    [Fact]
    public async Task CreateAsync_WithoutImageUrl_ReturnsNullNotError()
    {
        var service = CreateService();
        var request = new CreateProductRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Produto sem imagem",
            "electronics",
            99.90m,
            new ProductDimensionsDto(10m, 10m, 10m),
            1.0m,
            IsFragile: false,
            IsRestricted: false);

        var created = await service.CreateAsync(request, CancellationToken.None);
        var fetched = await service.GetBySkuIdAsync(request.SkuId, CancellationToken.None);

        Assert.Null(created.ImageUrl);
        Assert.NotNull(fetched);
        Assert.Null(fetched!.ImageUrl);
    }
}
