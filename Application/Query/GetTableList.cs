using Application.Storage;
using Domain.ValueObject;

namespace Application.Query;

public record GetTableListQuery : IQuery
{
    public bool HasPlayersOnly { get; init; } = false;
    public List<string>? Games { get; init; } = null;
    public decimal? MinStake { get; init; } = null;
    public decimal? MaxStake { get; init; } = null;
}

public record GetTableListResponse : IQueryResponse
{
    public required List<GetTableListResponseItem> Items { get; init; }
}

public record GetTableListResponseItem
{
    public required Guid Uid { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required decimal Stake { get; init; }
    public required int PlayerCount { get; init; }
}

public class GetTableListHandler(
    IStorage storage
) : IQueryHandler<GetTableListQuery, GetTableListResponse>
{
    public async Task<GetTableListResponse> HandleAsync(GetTableListQuery query)
    {
        var views = await storage.GetListViewsAsync(
            hasPlayersOnly: query.HasPlayersOnly,
            games: query.Games is not null ? query.Games.Select(DeserializeGame) : null,
            minStake: DeserializeMoney(query.MinStake),
            maxStake: DeserializeMoney(query.MaxStake)
        );

        return new GetTableListResponse
        {
            Items = views.Select(SerializeListView).ToList()
        };
    }

    private Game DeserializeGame(string value)
    {
        return (Game)Enum.Parse(typeof(Game), value);
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
            Game = view.Game.ToString(),
            MaxSeat = view.MaxSeat,
            Stake = view.Stake.Amount,
            PlayerCount = view.PlayerCount
        };
    }
}
