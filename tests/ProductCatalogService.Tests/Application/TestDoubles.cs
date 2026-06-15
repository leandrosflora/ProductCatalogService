using ProductCatalogService.Application.Ports;
using ProductCatalogService.Contracts;
using ProductCatalogService.Domain;

namespace ProductCatalogService.Tests.Application;

internal sealed class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _productsBySkuId = new();

    public int AddCalls { get; private set; }
    public int SaveChangesCalls { get; private set; }
    public IReadOnlyCollection<Product> Products => _productsBySkuId.Values;

    public Task<Product?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken)
    {
        _productsBySkuId.TryGetValue(skuId, out var product);
        return Task.FromResult(product);
    }

    public Task<IReadOnlyList<Product>> GetBySkuIdsAsync(IReadOnlyCollection<Guid> skuIds, CancellationToken cancellationToken)
    {
        var products = skuIds
            .Distinct()
            .Where(_productsBySkuId.ContainsKey)
            .Select(skuId => _productsBySkuId[skuId])
            .Where(product => product.Status == ProductStatus.Active)
            .ToArray();

        return Task.FromResult<IReadOnlyList<Product>>(products);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        AddCalls++;
        _productsBySkuId.Add(product.SkuId, product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }

    public void Seed(Product product)
    {
        _productsBySkuId.Add(product.SkuId, product);
    }
}

internal sealed class RecordingProductPhysicalInfoCache : IProductPhysicalInfoCache
{
    private readonly Dictionary<Guid, ProductPhysicalInfoResponse> _items = new();

    public int GetCalls { get; private set; }
    public int SetCalls { get; private set; }
    public int RemoveCalls { get; private set; }
    public List<(ProductPhysicalInfoResponse Product, TimeSpan Ttl)> SetEntries { get; } = [];
    public List<Guid> RemovedSkuIds { get; } = [];

    public Task<ProductPhysicalInfoResponse?> GetAsync(Guid skuId, CancellationToken cancellationToken)
    {
        GetCalls++;
        _items.TryGetValue(skuId, out var product);
        return Task.FromResult(product);
    }

    public Task SetAsync(ProductPhysicalInfoResponse product, TimeSpan ttl, CancellationToken cancellationToken)
    {
        SetCalls++;
        SetEntries.Add((product, ttl));
        _items[product.SkuId] = product;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid skuId, CancellationToken cancellationToken)
    {
        RemoveCalls++;
        RemovedSkuIds.Add(skuId);
        _items.Remove(skuId);
        return Task.CompletedTask;
    }

    public void Seed(ProductPhysicalInfoResponse product)
    {
        _items[product.SkuId] = product;
    }
}

internal sealed class RecordingEventPublisher : IEventPublisher
{
    public List<(string EventType, object Payload)> Events { get; } = [];

    public Task AddToOutboxAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        Events.Add((eventType, payload));
        return Task.CompletedTask;
    }
}
