using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Storage;

namespace Application.Test.UnitOfWork;

public class StubUnitOfWork(
    StubRepository repository,
    StubStorage storage,
    StubEventDispatcher eventDispatcher
) : Application.UnitOfWork.UnitOfWork(repository, storage, eventDispatcher)
{
    public readonly StubRepository Repository = repository;
    public readonly StubStorage Storage = storage;
    public readonly StubEventDispatcher EventDispatcher = eventDispatcher;
}
