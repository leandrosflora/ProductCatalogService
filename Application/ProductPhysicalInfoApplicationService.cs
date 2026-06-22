using ProductCatalogService.Application.Ports;
using ProductCatalogService.Contracts;
using ProductCatalogService.Domain;

namespace ProductCatalogService.Application;

public sealed class ProductPhysicalInfoApplicationService
{
    private static readonly TimeSpan PhysicalInfoCacheTtl = TimeSpan.FromHours(6);

    private readonly IProductRepository _repository;
    private readonly IProductPhysicalInfoCache _cache;

    public ProductPhysicalInfoApplicationService(IProductRepository repository, IProductPhysicalInfoCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<ProductLogisticsResponse?> GetBySkuIdAsync(
        Guid skuId,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync(skuId, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        var product = await _repository.GetBySkuIdAsync(skuId, cancellationToken);

        if (product is not { Status: ProductStatus.Active })
        {
            return null;
        }

        var logistics = MapLogistics(product);
        await _cache.SetAsync(logistics, PhysicalInfoCacheTtl, cancellationToken);

        return logistics;
    }

    public async Task<IReadOnlyList<ProductLogisticsResponse>> GetBatchAsync(
        IReadOnlyCollection<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        if (skuIds.Count == 0)
        {
            return [];
        }

        var distinctSkuIds = skuIds.Distinct().ToArray();
        var result = new List<ProductLogisticsResponse>(distinctSkuIds.Length);
        var missingSkuIds = new List<Guid>();

        foreach (var skuId in distinctSkuIds)
        {
            var cached = await _cache.GetAsync(skuId, cancellationToken);

            if (cached is not null)
            {
                result.Add(cached);
                continue;
            }

            missingSkuIds.Add(skuId);
        }

        if (missingSkuIds.Count == 0)
        {
            return result;
        }

        var products = await _repository.GetBySkuIdsAsync(missingSkuIds, cancellationToken);

        foreach (var product in products)
        {
            var logistics = MapLogistics(product);
            result.Add(logistics);

            await _cache.SetAsync(logistics, PhysicalInfoCacheTtl, cancellationToken);
        }

        return result;
    }

    private static ProductLogisticsResponse MapLogistics(Product product)
    {
        return new ProductLogisticsResponse(
            product.SkuId,
            product.SellerId,
            product.WeightKg,
            product.Dimensions.HeightCm,
            product.Dimensions.WidthCm,
            product.Dimensions.LengthCm,
            product.Category,
            BuildRestrictionCodes(product));
    }

    private static IReadOnlyList<string> BuildRestrictionCodes(Product product)
    {
        var restrictionCodes = new List<string>();

        if (product.IsFragile)
        {
            restrictionCodes.Add("FRAGILE");
        }

        if (product.IsRestricted)
        {
            restrictionCodes.Add("RESTRICTED");
        }

        return restrictionCodes;
    }
}
