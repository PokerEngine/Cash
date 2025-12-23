using Domain.Event;
using Domain.ValueObject;

namespace Application.Event;

public interface IEventHandler<in T> where T : IEvent
{
    Task HandleAsync(T @event, TableUid tableUid);
}
