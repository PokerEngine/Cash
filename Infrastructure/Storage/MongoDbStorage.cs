using Application.Exception;
using Application.Storage;
using Domain.Entity;
using Domain.ValueObject;
using Infrastructure.Client.MongoDb;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Infrastructure.Storage;

public class MongoDbStorage : IStorage
{
    private const string DetailViewCollectionName = "views_detail";
    private const string ListViewCollectionName = "views_list";

    private readonly IMongoCollection<DetailViewDocument> _detailViewCollection;
    private readonly IMongoCollection<ListViewDocument> _listViewCollection;

    public MongoDbStorage(MongoDbClient client, IOptions<MongoDbStorageOptions> options)
    {
        var db = client.Client.GetDatabase(options.Value.Database);

        _detailViewCollection = db.GetCollection<DetailViewDocument>(DetailViewCollectionName);
        _listViewCollection = db.GetCollection<ListViewDocument>(ListViewCollectionName);
    }

    public async Task<DetailView> GetDetailViewAsync(TableUid uid)
    {
        var document = await _detailViewCollection
            .Find(e => e.Uid == uid)
            .FirstOrDefaultAsync();

        if (document is null)
        {
            throw new TableNotFoundException("The table is not found");
        }

        return new DetailView
        {
            Uid = document.Uid,
            Game = document.Game,
            Stake = document.Stake,
            MaxSeat = document.MaxSeat,
            SmallBlind = document.SmallBlind,
            BigBlind = document.BigBlind,
            CurrentHandUid = document.CurrentHandUid,
            Players = document.Players.Select(p => new DetailViewPlayer
            {
                Nickname = p.Nickname,
                Seat = p.Seat,
                Stack = p.Stack,
                IsSittingOut = p.IsSittingOut
            }).ToList()
        };
    }

    public async Task<List<ListView>> GetListViewsAsync(
        bool hasPlayersOnly = false,
        IEnumerable<Game>? games = null,
        Money? minStake = null,
        Money? maxStake = null
    )
    {
        var filterBuilder = Builders<ListViewDocument>.Filter;
        var filter = filterBuilder.Empty;

        if (hasPlayersOnly)
        {
            filter &= filterBuilder.Gt(e => e.PlayerCount, 0);
        }

        if (games is not null)
        {
            filter &= filterBuilder.In(e => e.Game, games);
        }

        if (minStake is not null)
        {
            filter &= filterBuilder.Gte(e => e.Stake, minStake);
        }

        if (maxStake is not null)
        {
            filter &= filterBuilder.Lte(e => e.Stake, maxStake);
        }

        var documents = await _listViewCollection
            .Find(filter)
            .SortBy(e => e.Stake)
            .ThenBy(e => e.MaxSeat)
            .ThenBy(e => e.Game)
            .ToListAsync();

        return documents.Select(d => new ListView
        {
            Uid = d.Uid,
            Game = d.Game,
            MaxSeat = d.MaxSeat,
            Stake = d.Stake,
            PlayerCount = d.PlayerCount
        }).ToList();
    }

    public async Task SaveViewAsync(Table table)
    {
        await SaveDetailViewAsync(table);
        await SaveListViewAsync(table);
    }

    private async Task SaveDetailViewAsync(Table table)
    {
        var options = new FindOneAndReplaceOptions<DetailViewDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var document = new DetailViewDocument
        {
            Uid = table.Uid,
            Game = table.Game,
            MaxSeat = table.MaxSeat,
            Stake = table.BigBlind * table.ChipCost * 100,
            SmallBlind = table.SmallBlind * table.ChipCost,
            BigBlind = table.BigBlind * table.ChipCost,
            CurrentHandUid = table.IsHandInProgress() ? table.GetCurrentHandUid() : null,
            Players = table.Players.Select(p => new DetailViewDocumentPlayer
            {
                Nickname = p.Nickname,
                Seat = p.Seat,
                Stack = p.Stack * table.ChipCost,
                IsSittingOut = p.IsSittingOut
            }).ToList()
        };

        await _detailViewCollection.FindOneAndReplaceAsync(e => e.Uid == table.Uid, document, options);
    }

    private async Task SaveListViewAsync(Table table)
    {
        var options = new FindOneAndReplaceOptions<ListViewDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var document = new ListViewDocument
        {
            Uid = table.Uid,
            Game = table.Game,
            MaxSeat = table.MaxSeat,
            Stake = table.BigBlind * table.ChipCost * 100,
            PlayerCount = table.Players.Count()
        };

        await _listViewCollection.FindOneAndReplaceAsync(e => e.Uid == table.Uid, document, options);
    }
}

public class MongoDbStorageOptions
{
    public const string SectionName = "MongoDbStorage";

    public required string Database { get; init; }
}

internal sealed class DetailViewDocument
{
    [BsonId]
    public required TableUid Uid { get; init; }
    public required Game Game { get; init; }
    public required Seat MaxSeat { get; init; }
    public required Money Stake { get; init; }
    public required Money SmallBlind { get; init; }
    public required Money BigBlind { get; init; }
    public required HandUid? CurrentHandUid { get; init; }
    public required List<DetailViewDocumentPlayer> Players { get; init; }
}

internal sealed class DetailViewDocumentPlayer
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Money Stack { get; init; }
    public required bool IsSittingOut { get; init; }
}

internal sealed class ListViewDocument
{

    [BsonId]
    public required TableUid Uid { get; init; }
    public required Game Game { get; init; }
    public required Seat MaxSeat { get; init; }
    public required Money Stake { get; init; }
    public required int PlayerCount { get; init; }
}
