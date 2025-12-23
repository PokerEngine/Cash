using Domain.Event;
using Domain.ValueObject;

namespace Application.Event;

public interface IEventDispatcher
{
    Task DispatchAsync<T>(T @event, TableUid tableUid) where T : IEvent;
}
