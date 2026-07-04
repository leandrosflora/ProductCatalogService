using ProductCatalogService.Domain;

namespace ProductCatalogService.Application.Ports;

public interface IProductRepository
{
    Task<Product?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetBySkuIdsAsync(IReadOnlyCollection<Guid> skuIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
