using Domain.Event;
using Domain.ValueObject;

namespace Application.Repository;

public interface IRepository
{
    Task<TableUid> GetNextUidAsync();
    Task<List<IEvent>> GetEventsAsync(TableUid tableUid);
    Task AddEventsAsync(TableUid tableUid, List<IEvent> events);
}
