using Domain.Event;
using Domain.ValueObject;

namespace Application.Event;

public interface IEventDispatcher
{
    Task DispatchAsync(IEvent @event, EventContext context);
}
