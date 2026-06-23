using ProductCatalogService.Domain;
using ProductCatalogService.Infrastructure.Outbox;

namespace ProductCatalogService.Infrastructure.Persistence;

public sealed class ProductCatalogUnitOfWork
{
    private readonly List<Product> _newProducts = [];
    private readonly Dictionary<Guid, Product> _trackedProducts = [];
    private readonly List<OutboxMessage> _outboxMessages = [];

    public IReadOnlyCollection<Product> NewProducts => _newProducts;
    public IReadOnlyCollection<Product> TrackedProducts => _trackedProducts.Values;
    public IReadOnlyCollection<OutboxMessage> OutboxMessages => _outboxMessages;

    public void Track(Product product)
    {
        if (!_newProducts.Any(x => x.Id == product.Id))
        {
            _trackedProducts[product.Id] = product;
        }
    }

    public void Add(Product product)
    {
        _newProducts.Add(product);
        _trackedProducts.Remove(product.Id);
    }

    public void AddOutboxMessage(OutboxMessage message)
    {
        _outboxMessages.Add(message);
    }

    public void Clear()
    {
        _newProducts.Clear();
        _trackedProducts.Clear();
        _outboxMessages.Clear();
    }
}
