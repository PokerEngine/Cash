using Domain.Event;
using Domain.ValueObject;

namespace Application.Repository;

public interface IRepository
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task<TableUid> GetNextUidAsync();
    Task<IList<BaseEvent>> GetEventsAsync(TableUid tableUid);
    Task AddEventsAsync(TableUid tableUid, IList<BaseEvent> events);
}
