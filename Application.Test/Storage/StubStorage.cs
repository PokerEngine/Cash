using Application.Exception;
using Application.Storage;
using Domain.Entity;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Application.Test.Storage;

public class StubStorage : IStorage
{
    private readonly ConcurrentDictionary<TableUid, DetailView> _detailMapping = new();
    private readonly ConcurrentDictionary<TableUid, ListView> _listMapping = new();

    public Task<DetailView> GetDetailViewAsync(Guid tableUid)
    {
        if (!_detailMapping.TryGetValue(tableUid, out var view))
        {
            throw new TableNotFoundException("The table is not found");
        }

        return Task.FromResult(view);
    }

    public Task<List<ListView>> GetListViewsAsync(
        bool hasPlayersOnly = false,
        IEnumerable<string>? games = null,
        int? minStake = null,
        int? maxStake = null
    )
    {
        List<ListView> views = [];
        var gamesSet = (games is not null) ? games.ToHashSet() : new HashSet<string>();

        foreach (var view in _listMapping.Values)
        {
            if (hasPlayersOnly && view.PlayerCount == 0)
            {
                continue;
            }

            if (games is not null && !gamesSet.Contains(view.Rules.Game))
            {
                continue;
            }

            if (minStake is not null && view.Rules.Stake < minStake)
            {
                continue;
            }

            if (maxStake is not null && view.Rules.Stake > maxStake)
            {
                continue;
            }

            views.Add(view);
        }

        return Task.FromResult(views);
    }

    public Task SaveViewAsync(Table table)
    {
        SaveDetailView(table);
        SaveListView(table);
        return Task.CompletedTask;
    }

    private void SaveDetailView(Table table)
    {
        var view = new DetailView
        {
            Uid = table.Uid,
            Rules = new DetailViewRules
            {
                Game = table.Rules.Game.ToString(),
                MaxSeat = table.Rules.MaxSeat,
                SmallBlind = (table.Rules.SmallBlind * table.Rules.ChipCost).Amount,
                BigBlind = (table.Rules.BigBlind * table.Rules.ChipCost).Amount,
                Stake = (int)(table.Rules.BigBlind * table.Rules.ChipCost * 100).Amount
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
        _detailMapping.AddOrUpdate(table.Uid, view, (_, _) => view);
    }

    private void SaveListView(Table table)
    {
        var view = new ListView
        {
            Uid = table.Uid,
            Rules = new ListViewRules
            {
                Game = table.Rules.Game.ToString(),
                MaxSeat = table.Rules.MaxSeat,
                Stake = (int)(table.Rules.BigBlind * table.Rules.ChipCost * 100).Amount
            },
            PlayerCount = table.Players.Count()
        };
        _listMapping.AddOrUpdate(table.Uid, view, (_, _) => view);
    }
}
