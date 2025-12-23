using Application.IntegrationEvent;
using System.Collections.Concurrent;

namespace Infrastructure.IntegrationEvent;

public class InMemoryIntegrationEventQueue : IIntegrationEventQueue
{
    private sealed class ChannelQueue
    {
        public readonly ConcurrentQueue<IIntegrationEvent> Queue = new();
        public readonly SemaphoreSlim Signal = new(0);
    }

    private readonly ConcurrentDictionary<IntegrationEventChannel, ChannelQueue> _channels = new();

    public Task EnqueueAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventChannel channel,
        CancellationToken cancellationToken = default
    )
    {
        if (integrationEvent is null)
            throw new ArgumentNullException(nameof(integrationEvent));

        var channelQueue = GetChannelQueue(channel);

        channelQueue.Queue.Enqueue(integrationEvent);
        channelQueue.Signal.Release();

        return Task.CompletedTask;
    }

    public async Task<IIntegrationEvent> DequeueAsync(
        IntegrationEventChannel channel,
        CancellationToken cancellationToken = default
    )
    {
        var channelQueue = GetChannelQueue(channel);

        await channelQueue.Signal.WaitAsync(cancellationToken);

        if (channelQueue.Queue.TryDequeue(out var integrationEvent))
            return integrationEvent;

        // This should be extremely rare, but keeps correctness
        throw new InvalidOperationException($"Signaled dequeue for channel {channel} but no event was available");
    }

    private ChannelQueue GetChannelQueue(IntegrationEventChannel channel)
    {
        return _channels.GetOrAdd(
            channel,
            _ => new ChannelQueue()
        );
    }
}
