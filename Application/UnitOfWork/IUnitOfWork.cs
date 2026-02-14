using Domain.Entity;

namespace Application.UnitOfWork;

public interface IUnitOfWork
{
    void RegisterTable(Table table);
    Task CommitAsync(bool updateViews = true);
}
