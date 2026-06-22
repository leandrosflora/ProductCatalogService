using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Contracts;

namespace ProductCatalogService.Infrastructure.Cache;

public sealed class RedisProductPhysicalInfoCache : IProductPhysicalInfoCache
{
    private readonly IDistributedCache _cache;

    public RedisProductPhysicalInfoCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<ProductLogisticsResponse?> GetAsync(Guid skuId, CancellationToken cancellationToken)
    {
        var json = await _cache.GetStringAsync(BuildKey(skuId), cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProductLogisticsResponse>(json);
    }

    public async Task SetAsync(
        ProductLogisticsResponse product,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(product);

        await _cache.SetStringAsync(
            BuildKey(product.SkuId),
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    public Task RemoveAsync(Guid skuId, CancellationToken cancellationToken)
    {
        return _cache.RemoveAsync(BuildKey(skuId), cancellationToken);
    }

    private static string BuildKey(Guid skuId)
    {
        return $"product-logistics:{skuId:N}";
    }
}
