using Application.Exception;
using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using Infrastructure.Client.MongoDb;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Infrastructure.Repository;

public class MongoDbRepository : IRepository
{
    private readonly IMongoCollection<EventDocument> _collection;
    private const string Collection = "events";

    public MongoDbRepository(MongoDbClient client, IOptions<MongoDbRepositoryOptions> options)
    {
        var db = client.Client.GetDatabase(options.Value.Database);
        _collection = db.GetCollection<EventDocument>(Collection);
    }

    public Task<TableUid> GetNextUidAsync()
    {
        return Task.FromResult(new TableUid(Guid.NewGuid()));
    }

    public async Task<List<IEvent>> GetEventsAsync(TableUid tableUid)
    {
        var documents = await _collection
            .Find(e => e.TableUid == tableUid)
            .SortBy(e => e.Id)
            .ToListAsync();

        var events = new List<IEvent>();

        foreach (var document in documents)
        {
            var type = MongoDbEventTypeResolver.GetType(document.Type);
            var @event = (IEvent)BsonSerializer.Deserialize(document.Data, type);
            events.Add(@event);
        }

        if (events.Count == 0)
        {
            throw new TableNotFoundException("The table is not found");
        }

        return events;
    }

    public async Task AddEventsAsync(TableUid tableUid, List<IEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        var documents = events.Select(e => new EventDocument
        {
            Type = MongoDbEventTypeResolver.GetName(e),
            TableUid = tableUid,
            OccurredAt = e.OccurredAt,
            Data = e.ToBsonDocument(e.GetType())
        });

        await _collection.InsertManyAsync(documents);
    }
}

public class MongoDbRepositoryOptions
{
    public const string SectionName = "MongoDbRepository";

    public required string Database { get; init; }
}

internal sealed class EventDocument
{
    [BsonId]
    public ObjectId Id { get; init; }

    public required string Type { get; init; }
    public required TableUid TableUid { get; init; }
    public required DateTime OccurredAt { get; init; }
    public required BsonDocument Data { get; init; }
}
