using Microsoft.Extensions.DependencyInjection;

namespace Core.Shared.Events;

public interface IEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;

    Task DispatchImmediatelyAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;

    Task HandleDispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}

public class EventDispatcher(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory) : IEventDispatcher
{

    public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        IBackgroundTaskQueue<TEvent> backgroundTaskQueue = serviceProvider.GetService<IBackgroundTaskQueue<TEvent>>()
            ?? throw new ArgumentException(nameof(backgroundTaskQueue));
        await backgroundTaskQueue.QueueTaskAsync(@event, cancellationToken);
    }

    public async Task DispatchImmediatelyAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IEnumerable<IEventHandler<TEvent>> handlers = serviceScopeFactory.CreateScope().ServiceProvider.GetServices<IEventHandler<TEvent>>(); foreach (IEventHandler<TEvent> handler in handlers) { await handler.Handle(@event, cancellationToken); }
    }
    public async Task HandleDispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent => await DispatchImmediatelyAsync(@event, cancellationToken);
}