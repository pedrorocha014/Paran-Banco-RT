namespace Core.Shared.Events;

public interface IEvent;

public interface IEventHandler<in TEvent> : IEvent where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
