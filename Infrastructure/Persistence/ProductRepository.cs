using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Domain;

namespace ProductCatalogService.Infrastructure.Persistence;

public sealed class ProductRepository : IProductRepository
{
    private readonly ProductCatalogDbContext _dbContext;

    public ProductRepository(ProductCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .FirstOrDefaultAsync(x => x.SkuId == skuId, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetBySkuIdsAsync(
        IReadOnlyCollection<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Where(x => skuIds.Contains(x.SkuId))
            .Where(x => x.Status == ProductStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
