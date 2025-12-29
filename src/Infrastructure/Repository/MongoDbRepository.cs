using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Infrastructure.Repository;

public class MongoDbRepository : IRepository
{
    private readonly IMongoCollection<EventDocument> _collection;
    private const string Collection = "events";

    public MongoDbRepository(IOptions<MongoDbRepositoryOptions> options, ILogger<MongoDbRepository> logger)
    {
        var url = $"mongodb://{options.Value.Username}:{options.Value.Password}@{options.Value.Host}:{options.Value.Port}";
        var client = new MongoClient(url);
        var db = client.GetDatabase(options.Value.Database);
        _collection = db.GetCollection<EventDocument>(Collection);

        BsonSerializerConfig.Register();
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
            var type = Type.GetType(document.Type, throwOnError: true)!;
            var @event = (IEvent)BsonSerializer.Deserialize(document.Data, type);
            events.Add(@event);
        }

        if (events.Count == 0)
        {
            throw new InvalidOperationException("The table is not found");
        }

        return events;
    }

    public async Task AddEventsAsync(TableUid tableUid, List<IEvent> events)
    {
        var documents = events.Select(e => new EventDocument
        {
            Type = e.GetType().AssemblyQualifiedName!,
            TableUid = tableUid,
            OccurredAt = e.OccuredAt,
            Data = e.ToBsonDocument(e.GetType())
        });

        await _collection.InsertManyAsync(documents);
    }
}

public class MongoDbRepositoryOptions
{
    public const string SectionName = "MongoDB";

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
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

internal static class BsonSerializerConfig
{
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.TryRegisterSerializer(new TableUidSerializer());
        BsonSerializer.TryRegisterSerializer(new HandUidSerializer());
        BsonSerializer.TryRegisterSerializer(new NicknameSerializer());
        BsonSerializer.TryRegisterSerializer(new SeatSerializer());
        BsonSerializer.TryRegisterSerializer(new ChipsSerializer());
        BsonSerializer.TryRegisterSerializer(new MoneySerializer());
    }
}

internal sealed class TableUidSerializer : SerializerBase<TableUid>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TableUid value)
        => context.Writer.WriteGuid(value);

    public override TableUid Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => context.Reader.ReadGuid();
}

internal sealed class HandUidSerializer : SerializerBase<HandUid>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, HandUid value)
        => context.Writer.WriteGuid(value);

    public override HandUid Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => context.Reader.ReadGuid();
}

internal sealed class NicknameSerializer : SerializerBase<Nickname>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Nickname value)
        => context.Writer.WriteString(value);

    public override Nickname Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => context.Reader.ReadString();
}

internal sealed class SeatSerializer : SerializerBase<Seat>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Seat value)
        => context.Writer.WriteInt32(value);

    public override Seat Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => context.Reader.ReadInt32();
}

internal sealed class ChipsSerializer : SerializerBase<Chips>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Chips value)
        => context.Writer.WriteInt32(value);

    public override Chips Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => context.Reader.ReadInt32();
}

internal sealed class MoneySerializer : SerializerBase<Money>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Money value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("amount");
        context.Writer.WriteDecimal128(value.Amount);
        context.Writer.WriteName("currency");
        context.Writer.WriteString(value.Currency.ToString());
        context.Writer.WriteEndDocument();
    }

    public override Money Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        decimal amount = default;
        Currency currency = default;

        context.Reader.ReadStartDocument();

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = context.Reader.ReadName(Utf8NameDecoder.Instance);

            switch (name)
            {
                case "amount":
                    {
                        var value = context.Reader.ReadDecimal128();
                        amount = Decimal128.ToDecimal(value);
                        break;
                    }

                case "currency":
                    currency = Enum.Parse<Currency>(
                        context.Reader.ReadString(),
                        ignoreCase: true
                    );
                    break;

                default:
                    context.Reader.SkipValue();
                    break;
            }
        }

        context.Reader.ReadEndDocument();

        return new Money(amount, currency);
    }
}
