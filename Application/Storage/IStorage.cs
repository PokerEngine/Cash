using Domain.Entity;
using Domain.ValueObject;

namespace Application.Storage;

public interface IStorage
{
    Task<DetailView> GetDetailViewAsync(Guid tableUid);
    Task<List<ListView>> GetListViewsAsync(
        bool hasPlayersOnly = false,
        IEnumerable<string>? games = null,
        int? minStake = null,
        int? maxStake = null
    );
    Task SaveViewAsync(Table table);
}

public record DetailView
{
    public required Guid Uid { get; init; }
    public required DetailViewRules Rules { get; init; }
    public required Guid? CurrentHandUid { get; init; }
    public required List<DetailViewPlayer> Players { get; init; }
}

public record DetailViewRules
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int Stake { get; init; }
    public required decimal SmallBlind { get; init; }
    public required decimal BigBlind { get; init; }
}

public record DetailViewPlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required decimal Stack { get; init; }
    public required bool IsSittingOut { get; init; }
}

public record ListView
{
    public required Guid Uid { get; init; }
    public required ListViewRules Rules { get; init; }
    public required int PlayerCount { get; init; }
}

public record ListViewRules
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int Stake { get; init; }
}
