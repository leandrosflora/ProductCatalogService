using ProductCatalogService.Application.Ports;
using ProductCatalogService.Contracts;

namespace ProductCatalogService.Infrastructure.Mocking;

public sealed class MockProductPhysicalInfoCache : IProductPhysicalInfoCache
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, CacheEntry> _entries = new();

    public Task<ProductPhysicalInfoResponse?> GetAsync(Guid skuId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(skuId, out var entry))
            {
                return Task.FromResult<ProductPhysicalInfoResponse?>(null);
            }

            if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _entries.Remove(skuId);

                return Task.FromResult<ProductPhysicalInfoResponse?>(null);
            }

            return Task.FromResult<ProductPhysicalInfoResponse?>(entry.Product);
        }
    }

    public Task SetAsync(ProductPhysicalInfoResponse product, TimeSpan ttl, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _entries[product.SkuId] = new CacheEntry(product, DateTimeOffset.UtcNow.Add(ttl));
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid skuId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _entries.Remove(skuId);
        }

        return Task.CompletedTask;
    }

    private sealed record CacheEntry(ProductPhysicalInfoResponse Product, DateTimeOffset ExpiresAt);
}
