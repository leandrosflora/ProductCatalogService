using ProductCatalogService.Application.Ports;
using ProductCatalogService.Domain;

namespace ProductCatalogService.Infrastructure.Mocking;

public sealed class MockProductRepository : IProductRepository
{
    private readonly MockProductCatalogStore _store;

    public MockProductRepository(MockProductCatalogStore store)
    {
        _store = store;
    }

    public Task<Product?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.Get(skuId));
    }

    public Task<IReadOnlyList<Product>> GetBySkuIdsAsync(
        IReadOnlyCollection<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.GetActiveProducts(skuIds));
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _store.Add(product);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
