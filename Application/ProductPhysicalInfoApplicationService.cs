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

    public async Task<IReadOnlyList<ProductPhysicalInfoResponse>> GetBatchAsync(
        BatchPhysicalInfoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SkuIds.Count == 0)
        {
            return [];
        }

        var distinctSkuIds = request.SkuIds.Distinct().ToArray();
        var result = new List<ProductPhysicalInfoResponse>(distinctSkuIds.Length);
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
            var physicalInfo = MapPhysicalInfo(product);
            result.Add(physicalInfo);

            await _cache.SetAsync(physicalInfo, PhysicalInfoCacheTtl, cancellationToken);
        }

        return result;
    }

    private static ProductPhysicalInfoResponse MapPhysicalInfo(Product product)
    {
        return new ProductPhysicalInfoResponse(
            product.SkuId,
            product.SellerId,
            product.WeightKg,
            product.Dimensions.HeightCm,
            product.Dimensions.WidthCm,
            product.Dimensions.LengthCm,
            product.Category,
            product.IsFragile,
            product.IsRestricted,
            product.Status.ToString());
    }
}
