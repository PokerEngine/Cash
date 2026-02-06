using Domain.Entity;
using Domain.ValueObject;

namespace Application.Storage;

public interface IStorage
{
    Task<DetailView> GetDetailViewAsync(TableUid tableUid);
    Task<List<ListView>> GetListViewsAsync(
        bool hasPlayersOnly = false,
        IEnumerable<Game>? games = null,
        Money? minStake = null,
        Money? maxStake = null
    );
    Task SaveViewAsync(Table table);
}

public record DetailView
{
    public required TableUid Uid { get; init; }
    public required Game Game { get; init; }
    public required Seat MaxSeat { get; init; }
    public required Money Stake { get; init; }
    public required Money SmallBlind { get; init; }
    public required Money BigBlind { get; init; }
    public required HandUid? CurrentHandUid { get; init; }
    public required List<DetailViewPlayer> Players { get; init; }
}

public record DetailViewPlayer
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Money Stack { get; init; }
    public required bool IsSittingOut { get; init; }
}

public record ListView
{
    public required TableUid Uid { get; init; }
    public required Game Game { get; init; }
    public required Seat MaxSeat { get; init; }
    public required Money Stake { get; init; }
    public required int PlayerCount { get; init; }
}
