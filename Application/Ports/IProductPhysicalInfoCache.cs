using ProductCatalogService.Contracts;

namespace ProductCatalogService.Application.Ports;

public interface IProductPhysicalInfoCache
{
    Task<ProductLogisticsResponse?> GetAsync(Guid skuId, CancellationToken cancellationToken);

    Task SetAsync(ProductLogisticsResponse product, TimeSpan ttl, CancellationToken cancellationToken);

    Task RemoveAsync(Guid skuId, CancellationToken cancellationToken);
}
