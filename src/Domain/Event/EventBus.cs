
namespace Domain.Event;

public interface IEventBus
{
    void Subscribe<T>(Action<T> listener) where T : BaseEvent;
    void Unsubscribe<T>(Action<T> listener) where T : BaseEvent;
    void Publish<T>(T @event) where T : BaseEvent;
}

public class EventBus : IEventBus
{
    private readonly List<Delegate> _listeners = [];

    public void Subscribe<T>(Action<T> listener) where T : BaseEvent
    {
        _listeners.Add(listener);
    }

    public void Unsubscribe<T>(Action<T> listener) where T : BaseEvent
    {
        _listeners.Remove(listener);
    }

    public void Publish<T>(T @event) where T : BaseEvent
    {
        foreach (var listener in _listeners)
        {
            if (listener is Action<T> typedListener)
            {
                typedListener(@event);
            }
        }
    }
}
