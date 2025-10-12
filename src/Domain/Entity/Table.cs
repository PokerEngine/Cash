using Domain.Event;
using Domain.ValueObject;

namespace Domain.Entity;

public class Table
{
    public readonly TableUid Uid;
    public readonly Game Game;
    public readonly Money ChipCost;
    public readonly Chips SmallBlind;
    public readonly Chips BigBlind;
    public readonly Seat MaxSeat;

    public Seat SmallBlindSeat { get; private set; }
    public Seat BigBlindSeat { get; private set; }
    public Seat ButtonSeat { get; private set; }
    public HandUid? CurrentHandUid { get; private set; }

    private readonly Player?[] _players;

    public IEnumerable<Player> Players => _players.OfType<Player>();
    public IEnumerable<Player> ActivePlayers => Players.Where(x => x.IsActive);

    private Table(
        TableUid uid,
        Game game,
        Chips smallBlind,
        Chips bigBlind,
        Money chipCost,
        Seat maxSeat,
        Seat smallBlindSeat,
        Seat bigBlindSeat,
        Seat buttonSeat,
        IEnumerable<Player> players
    )
    {
        Uid = uid;
        Game = game;
        SmallBlind = smallBlind;
        BigBlind = bigBlind;
        ChipCost = chipCost;

        MaxSeat = maxSeat;
        SmallBlindSeat = smallBlindSeat;
        BigBlindSeat = bigBlindSeat;
        ButtonSeat = buttonSeat;

        _players = new Player?[maxSeat];

        var allPlayers = players.ToList();
        foreach (var player in allPlayers)
        {
            if (player.Seat > maxSeat)
            {
                throw new ArgumentOutOfRangeException(nameof(players), players, $"The table supports seats till {maxSeat}");
            }
            _players[player.Seat - 1] = player;
        }

        var nicknames = allPlayers.Select(x => x.Nickname).ToHashSet();
        if (allPlayers.Count != nicknames.Count)
        {
            throw new ArgumentException("The table must contain players with unique nicknames", nameof(players));
        }

        var seats = allPlayers.Select(x => x.Seat).ToHashSet();
        if (allPlayers.Count != seats.Count)
        {
            throw new ArgumentException("The table must contain players with unique seats", nameof(players));
        }

        if (SmallBlindSeat == BigBlindSeat)
        {
            throw new ArgumentException("The table must contain different players on the big and small blinds", nameof(smallBlindSeat));
        }

        if (ButtonSeat == BigBlindSeat)
        {
            throw new ArgumentException("The table must contain different players on the big blind and button", nameof(buttonSeat));
        }
    }

    public static Table FromScratch(
        TableUid uid,
        Game game,
        Seat maxSeat,
        Chips smallBlind,
        Chips bigBlind,
        Money chipCost,
        IEventBus eventBus
    )
    {
        var smallBlindSeat = maxSeat == new Seat(2) ? new Seat(2) : new Seat(1);
        var bigBlindSeat = maxSeat == new Seat(2) ? new Seat(1) : new Seat(2);
        var table = new Table(
            uid: uid,
            game: game,
            smallBlind: smallBlind,
            bigBlind: bigBlind,
            chipCost: chipCost,
            maxSeat: maxSeat,
            smallBlindSeat: smallBlindSeat,
            bigBlindSeat: bigBlindSeat,
            buttonSeat: maxSeat,
            players: []
        );

        var @event = new TableIsCreatedEvent(
            Uid: uid,
            Game: game,
            SmallBlind: smallBlind,
            BigBlind: bigBlind,
            ChipCost: chipCost,
            MaxSeat: maxSeat,
            OccuredAt: DateTime.Now
        );
        eventBus.Publish(@event);

        return table;
    }

    public void SitDown(
        Nickname nickname,
        Seat seat,
        Chips stack,
        EventBus eventBus
    )
    {
        if (seat > MaxSeat)
        {
            throw new InvalidOperationException($"The table supports seats till {MaxSeat}");
        }

        var player = GetPlayerBySeat(seat);
        if (player is not null)
        {
            throw new InvalidOperationException("The seat has already been occupied at the table");
        }

        player = GetPlayerByNickname(nickname);
        if (player is not null)
        {
            throw new InvalidOperationException("A player with the given nickname is already sitting down at the table");
        }

        _players[seat - 1] = new Player(
            nickname: nickname,
            seat: seat,
            stack: stack,
            isDisconnected: false,
            isSittingOut: false,
            isWaitingForBigBlind: true
        );

        var @event = new PlayerSatDownEvent(
            Nickname: nickname,
            Seat: seat,
            Stack: stack,
            OccuredAt: DateTime.Now
        );
        eventBus.Publish(@event);
    }

    public void StandUp(
        Nickname nickname,
        EventBus eventBus
    )
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new InvalidOperationException("A player with the given nickname is not found at the table");
        }

        _players[player.Seat - 1] = null;

        var @event = new PlayerStoodUpEvent(
            Nickname: nickname,
            OccuredAt: DateTime.Now
        );
        eventBus.Publish(@event);
    }

    public void SitOut(
        Nickname nickname,
        EventBus eventBus
    )
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new InvalidOperationException("A player with the given nickname is not found at the table");
        }

        player.SitOut();

        var @event = new PlayerSatOutEvent(
            Nickname: nickname,
            OccuredAt: DateTime.Now
        );
        eventBus.Publish(@event);
    }

    public void SitIn(
        Nickname nickname,
        EventBus eventBus
    )
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new InvalidOperationException("A player with the given nickname is not found at the table");
        }

        player.SitIn();

        var @event = new PlayerSatInEvent(
            Nickname: nickname,
            OccuredAt: DateTime.Now
        );
        eventBus.Publish(@event);
    }

    private Player? GetPlayerByNickname(Nickname nickname)
    {
        return Players.FirstOrDefault(x => x.Nickname == nickname);
    }

    private Player? GetPlayerBySeat(Seat seat)
    {
        return Players.FirstOrDefault(x => x.Seat == seat);
    }
}
