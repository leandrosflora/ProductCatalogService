using ProductCatalogService.Application.Ports;
using ProductCatalogService.Contracts;
using ProductCatalogService.Domain;

namespace ProductCatalogService.Application;

public sealed class ProductApplicationService
{
    private readonly IProductRepository _repository;
    private readonly IProductPhysicalInfoCache _cache;
    private readonly IEventPublisher _eventPublisher;

    public ProductApplicationService(
        IProductRepository repository,
        IProductPhysicalInfoCache cache,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _cache = cache;
        _eventPublisher = eventPublisher;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetBySkuIdAsync(request.SkuId, cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("Product already exists for this SKU");
        }

        var product = new Product(
            request.SellerId,
            request.SkuId,
            request.Title,
            request.Category,
            request.Price,
            new ProductDimensions(
                request.Dimensions.HeightCm,
                request.Dimensions.WidthCm,
                request.Dimensions.LengthCm),
            request.WeightKg,
            request.IsFragile,
            request.IsRestricted);

        await _repository.AddAsync(product, cancellationToken);
        await AddProductEventAsync("ProductCreated", product, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(product);
    }

    public async Task<ProductResponse?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken)
    {
        var product = await _repository.GetBySkuIdAsync(skuId, cancellationToken);

        return product is null ? null : Map(product);
    }

    public async Task<ProductResponse> UpdatePhysicalInfoAsync(
        Guid skuId,
        UpdatePhysicalInfoRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _repository.GetBySkuIdAsync(skuId, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Product not found");
        }

        product.UpdatePhysicalInfo(
            request.WeightKg,
            new ProductDimensions(
                request.Dimensions.HeightCm,
                request.Dimensions.WidthCm,
                request.Dimensions.LengthCm),
            request.IsFragile,
            request.IsRestricted);

        await AddProductEventAsync("ProductPhysicalInfoChanged", product, cancellationToken);
        await _cache.RemoveAsync(skuId, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(product);
    }

    public async Task<ProductResponse> ChangeStatusAsync(
        Guid skuId,
        ChangeProductStatusRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _repository.GetBySkuIdAsync(skuId, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Product not found");
        }

        if (!Enum.TryParse<ProductStatus>(request.Status, ignoreCase: true, out var status))
        {
            throw new ArgumentException("Invalid product status", nameof(request));
        }

        product.ChangeStatus(status);

        await AddProductEventAsync("ProductStatusChanged", product, cancellationToken);
        await _cache.RemoveAsync(skuId, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(product);
    }

    private Task AddProductEventAsync(string eventType, Product product, CancellationToken cancellationToken)
    {
        return _eventPublisher.AddToOutboxAsync(
            eventType,
            new
            {
                product.SkuId,
                product.SellerId,
                product.Category,
                product.WeightKg,
                product.Dimensions.HeightCm,
                product.Dimensions.WidthCm,
                product.Dimensions.LengthCm,
                product.IsFragile,
                product.IsRestricted,
                Status = product.Status.ToString(),
                product.UpdatedAt
            },
            cancellationToken);
    }

    private static ProductResponse Map(Product product)
    {
        return new ProductResponse(
            product.SkuId,
            product.SellerId,
            product.Title,
            product.Category,
            product.Price,
            product.Status.ToString(),
            product.WeightKg,
            product.Dimensions.HeightCm,
            product.Dimensions.WidthCm,
            product.Dimensions.LengthCm,
            product.IsFragile,
            product.IsRestricted);
    }
}
