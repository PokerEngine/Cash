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
        BsonSerializer.TryRegisterSerializer(new RulesSerializer());
        BsonSerializer.TryRegisterSerializer(new PositionsSerializer());
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

internal sealed class RulesSerializer : SerializerBase<Rules>
{
    private const string GameField = "game";
    private const string MaxSeatField = "maxSeat";
    private const string SmallBlindField = "smallBlind";
    private const string BigBlindField = "bigBlind";
    private const string ChipCostField = "chipCost";

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Rules value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName(GameField);
        context.Writer.WriteString(value.Game.ToString());
        context.Writer.WriteName(MaxSeatField);
        BsonSerializer.Serialize(context.Writer, value.MaxSeat);
        context.Writer.WriteName(SmallBlindField);
        BsonSerializer.Serialize(context.Writer, value.SmallBlind);
        context.Writer.WriteName(BigBlindField);
        BsonSerializer.Serialize(context.Writer, value.BigBlind);
        context.Writer.WriteName(ChipCostField);
        BsonSerializer.Serialize(context.Writer, value.ChipCost);
        context.Writer.WriteEndDocument();
    }

    public override Rules Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        Game game = default;
        Seat maxSeat = default;
        Chips smallBlind = default;
        Chips bigBlind = default;
        Money chipCost = default;

        context.Reader.ReadStartDocument();

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = context.Reader.ReadName(Utf8NameDecoder.Instance);

            switch (name)
            {
                case GameField:
                    game = Enum.Parse<Game>(
                        context.Reader.ReadString(),
                        ignoreCase: true
                    );
                    break;

                case MaxSeatField:
                    maxSeat = BsonSerializer.Deserialize<Seat>(context.Reader);
                    break;

                case SmallBlindField:
                    smallBlind = BsonSerializer.Deserialize<Chips>(context.Reader);
                    break;

                case BigBlindField:
                    bigBlind = BsonSerializer.Deserialize<Chips>(context.Reader);
                    break;

                case ChipCostField:
                    chipCost = BsonSerializer.Deserialize<Money>(context.Reader);
                    break;

                default:
                    context.Reader.SkipValue();
                    break;
            }
        }

        context.Reader.ReadEndDocument();

        return new Rules
        {
            Game = game,
            MaxSeat = maxSeat,
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            ChipCost = chipCost
        };
    }
}

internal sealed class PositionsSerializer : SerializerBase<Positions>
{
    private const string SmallBlindSeatField = "smallBlindSeat";
    private const string BigBlindSeatField = "bigBlindSeat";
    private const string ButtonSeatField = "buttonSeat";

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Positions value)
    {
        context.Writer.WriteStartDocument();

        if (value.SmallBlindSeat is not null)
        {
            context.Writer.WriteName(SmallBlindSeatField);
            BsonSerializer.Serialize(context.Writer, value.SmallBlindSeat);
        }

        context.Writer.WriteName(BigBlindSeatField);
        BsonSerializer.Serialize(context.Writer, value.BigBlindSeat);
        context.Writer.WriteName(ButtonSeatField);
        BsonSerializer.Serialize(context.Writer, value.ButtonSeat);
        context.Writer.WriteEndDocument();
    }

    public override Positions Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        Seat? smallBlindSeat = null;
        Seat bigBlindSeat = default;
        Seat buttonSeat = default;

        context.Reader.ReadStartDocument();

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = context.Reader.ReadName(Utf8NameDecoder.Instance);

            switch (name)
            {
                case SmallBlindSeatField:
                    smallBlindSeat = BsonSerializer.Deserialize<Seat>(context.Reader);
                    break;

                case BigBlindSeatField:
                    bigBlindSeat = BsonSerializer.Deserialize<Seat>(context.Reader);
                    break;

                case ButtonSeatField:
                    buttonSeat = BsonSerializer.Deserialize<Seat>(context.Reader);
                    break;

                default:
                    context.Reader.SkipValue();
                    break;
            }
        }

        context.Reader.ReadEndDocument();

        return new Positions
        {
            SmallBlindSeat = smallBlindSeat,
            BigBlindSeat = bigBlindSeat,
            ButtonSeat = buttonSeat
        };
    }
}
