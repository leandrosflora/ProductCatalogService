using ProductCatalogService.Application.Ports;
using ProductCatalogService.Contracts;

namespace ProductCatalogService.Infrastructure.Mocking;

public sealed class MockProductPhysicalInfoCache : IProductPhysicalInfoCache
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, CacheEntry> _entries = new();

    public Task<ProductLogisticsResponse?> GetAsync(Guid skuId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(skuId, out var entry))
            {
                return Task.FromResult<ProductLogisticsResponse?>(null);
            }

            if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _entries.Remove(skuId);

                return Task.FromResult<ProductLogisticsResponse?>(null);
            }

            return Task.FromResult<ProductLogisticsResponse?>(entry.Product);
        }
    }

    public Task SetAsync(ProductLogisticsResponse product, TimeSpan ttl, CancellationToken cancellationToken)
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

    private sealed record CacheEntry(ProductLogisticsResponse Product, DateTimeOffset ExpiresAt);
}
