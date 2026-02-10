using Domain.Entity;

namespace Application.Service.HandManager;

public interface IHandManager
{
    Task StartHandAsync(Table table);
}
