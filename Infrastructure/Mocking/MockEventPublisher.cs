using ProductCatalogService.Application.Ports;

namespace ProductCatalogService.Infrastructure.Mocking;

public sealed class MockEventPublisher : IEventPublisher
{
    public Task AddToOutboxAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
