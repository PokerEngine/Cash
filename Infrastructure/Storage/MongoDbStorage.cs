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

    public async Task<DetailView> GetDetailViewAsync(Guid uid)
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
            Rules = document.Rules,
            CurrentHandUid = document.CurrentHandUid,
            Players = document.Players
        };
    }

    public async Task<List<ListView>> GetListViewsAsync(
        bool hasPlayersOnly = false,
        IEnumerable<string>? games = null,
        int? minStake = null,
        int? maxStake = null
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
            filter &= filterBuilder.In(e => e.Rules.Game, games);
        }

        if (minStake is not null)
        {
            filter &= filterBuilder.Gte(e => e.Rules.Stake, minStake);
        }

        if (maxStake is not null)
        {
            filter &= filterBuilder.Lte(e => e.Rules.Stake, maxStake);
        }

        var documents = await _listViewCollection
            .Find(filter)
            .SortBy(e => e.Rules.Stake)
            .ThenBy(e => e.Rules.MaxSeat)
            .ThenBy(e => e.Rules.Game)
            .ToListAsync();

        return documents.Select(d => new ListView
        {
            Uid = d.Uid,
            Rules = d.Rules,
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
            Rules = new DetailViewRules
            {
                Game = table.Rules.Game.ToString(),
                MaxSeat = table.Rules.MaxSeat,
                Stake = (int)(table.Rules.BigBlind * table.Rules.ChipCost * 100).Amount,
                SmallBlind = (table.Rules.SmallBlind * table.Rules.ChipCost).Amount,
                BigBlind = (table.Rules.BigBlind * table.Rules.ChipCost).Amount
            },
            CurrentHandUid = table.IsHandInProgress() ? table.GetCurrentHandUid() : null,
            Players = table.Players.Select(p => new DetailViewPlayer
            {
                Nickname = p.Nickname,
                Seat = p.Seat,
                Stack = (p.Stack * table.Rules.ChipCost).Amount,
                IsSittingOut = p.IsSittingOut
            }).ToList()
        };

        await _detailViewCollection.FindOneAndReplaceAsync(e => e.Uid == (Guid)table.Uid, document, options);
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
            Rules = new ListViewRules
            {
                Game = table.Rules.Game.ToString(),
                MaxSeat = table.Rules.MaxSeat,
                Stake = (int)(table.Rules.BigBlind * table.Rules.ChipCost * 100).Amount,
            },
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
    public required Guid Uid { get; init; }
    public required DetailViewRules Rules { get; init; }
    public required Guid? CurrentHandUid { get; init; }
    public required List<DetailViewPlayer> Players { get; init; }
}

internal sealed class ListViewDocument
{

    [BsonId]
    public required TableUid Uid { get; init; }
    public required ListViewRules Rules { get; init; }
    public required int PlayerCount { get; init; }
}
