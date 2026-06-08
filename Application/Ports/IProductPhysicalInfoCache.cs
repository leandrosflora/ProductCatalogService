using ProductCatalogService.Contracts;

namespace ProductCatalogService.Application.Ports;

public interface IProductPhysicalInfoCache
{
    Task<ProductPhysicalInfoResponse?> GetAsync(Guid skuId, CancellationToken cancellationToken);

    Task SetAsync(ProductPhysicalInfoResponse product, TimeSpan ttl, CancellationToken cancellationToken);

    Task RemoveAsync(Guid skuId, CancellationToken cancellationToken);
}
