using System.Text.Json;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Infrastructure.Persistence;

namespace ProductCatalogService.Infrastructure.Outbox;

public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly ProductCatalogDbContext _dbContext;

    public OutboxEventPublisher(ProductCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddToOutboxAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        var message = new OutboxMessage(eventType, json);

        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }
}
