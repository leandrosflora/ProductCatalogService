using ProductCatalogService.Domain;

namespace ProductCatalogService.Infrastructure.Mocking;

public sealed class MockProductCatalogStore
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, Product> _products = new();

    public MockProductCatalogStore()
    { 

        Seed(new Product(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            "Smartphone Mock 128GB",
            "Electronics",
            1999.90m,
            new ProductDimensions(15.8m, 7.5m, 0.9m),
            0.19m,
            isFragile: true,
            isRestricted: false));

        Seed(new Product(
           Guid.Parse("11111111-1111-1111-1111-111111111111"),
           Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
           "Smartphone Mock 128GB",
           "Electronics",
           1999.90m,
           new ProductDimensions(15.8m, 7.5m, 0.9m),
           0.19m,
           isFragile: true,
           isRestricted: false));

        Seed(new Product(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Tênis Mock Runner",
            "Shoes",
            349.90m,
            new ProductDimensions(12m, 22m, 34m),
            0.85m,
            isFragile: false,
            isRestricted: false));

        var pausedProduct = new Product(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "Perfume Mock 100ml",
            "Beauty",
            229.90m,
            new ProductDimensions(14m, 6m, 6m),
            0.35m,
            isFragile: true,
            isRestricted: true);
        pausedProduct.ChangeStatus(ProductStatus.Paused);
        Seed(pausedProduct);
    }

    public Product? Get(Guid skuId)
    {
        lock (_lock)
        {
            return _products.GetValueOrDefault(skuId);
        }
    }

    public IReadOnlyList<Product> GetActiveProducts(IReadOnlyCollection<Guid> skuIds)
    {
        lock (_lock)
        {
            return skuIds
                .Select(skuId => _products.GetValueOrDefault(skuId))
                .Where(product => product is { Status: ProductStatus.Active })
                .Select(product => product!)
                .ToArray();
        }
    }

    public void Add(Product product)
    {
        lock (_lock)
        {
            _products[product.SkuId] = product;
        }
    }

    private void Seed(Product product)
    {
        _products.Add(product.SkuId, product);
    }
}
