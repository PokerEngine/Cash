using Domain.ValueObject;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Infrastructure.Client.MongoDb;

public class MongoDbClient
{
    public MongoClient Client;
    public MongoDbClient(IOptions<MongoDbClientOptions> options)
    {
        var url = $"mongodb://{options.Value.Username}:{options.Value.Password}@{options.Value.Host}:{options.Value.Port}";
        Client = new MongoClient(url);

        MongoDbSerializerConfig.Register();
    }
}

public class MongoDbClientOptions
{
    public const string SectionName = "MongoDb";

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}

internal static class MongoDbSerializerConfig
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
