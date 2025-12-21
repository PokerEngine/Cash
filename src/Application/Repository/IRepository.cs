using Domain.Event;
using Domain.ValueObject;

namespace Application.Repository;

public interface IRepository
{
    Task<TableUid> GetNextUidAsync();
    Task<List<BaseEvent>> GetEventsAsync(TableUid tableUid);
    Task AddEventsAsync(TableUid tableUid, List<BaseEvent> events);
}
