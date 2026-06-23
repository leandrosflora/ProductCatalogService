using System.Text.Json;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Infrastructure.Persistence;

namespace ProductCatalogService.Infrastructure.Outbox;

public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly ProductCatalogUnitOfWork _unitOfWork;

    public OutboxEventPublisher(ProductCatalogUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task AddToOutboxAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        var message = new OutboxMessage(eventType, json);

        _unitOfWork.AddOutboxMessage(message);
        return Task.CompletedTask;
    }
}
