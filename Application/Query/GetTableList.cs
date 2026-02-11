using Application.Storage;
using Domain.ValueObject;

namespace Application.Query;

public record GetTableListQuery : IQuery
{
    public bool HasPlayersOnly { get; init; } = false;
    public List<string>? Games { get; init; } = null;
    public int? MinStake { get; init; } = null;
    public int? MaxStake { get; init; } = null;
}

public record GetTableListResponse : IQueryResponse
{
    public required List<GetTableListResponseItem> Items { get; init; }
}

public record GetTableListResponseItem
{
    public required Guid Uid { get; init; }
    public required GetTableListResponseRules Rules { get; init; }
    public required int PlayerCount { get; init; }
}

public record GetTableListResponseRules
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required decimal Stake { get; init; }
}

public class GetTableListHandler(
    IStorage storage
) : IQueryHandler<GetTableListQuery, GetTableListResponse>
{
    public async Task<GetTableListResponse> HandleAsync(GetTableListQuery query)
    {
        var views = await storage.GetListViewsAsync(
            hasPlayersOnly: query.HasPlayersOnly,
            games: query.Games,
            minStake: query.MinStake,
            maxStake: query.MaxStake
        );

        return new GetTableListResponse
        {
            Items = views.Select(SerializeListView).ToList()
        };
    }

    private Money? DeserializeMoney(decimal? amount)
    {
        if (amount is null)
        {
            return null;
        }

        return new Money((decimal)amount, Currency.Usd);
    }

    private GetTableListResponseItem SerializeListView(ListView view)
    {
        return new GetTableListResponseItem
        {
            Uid = view.Uid,
            Rules = SerializeRules(view.Rules),
            PlayerCount = view.PlayerCount
        };
    }

    private GetTableListResponseRules SerializeRules(ListViewRules rules)
    {
        return new GetTableListResponseRules
        {
            Game = rules.Game,
            MaxSeat = rules.MaxSeat,
            Stake = rules.Stake
        };
    }
}
