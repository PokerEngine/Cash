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

    public Seat? SmallBlindSeat { get; private set; }
    public Seat? BigBlindSeat { get; private set; }
    public Seat? ButtonSeat { get; private set; }
    public HandUid? HandUid { get; private set; }

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
        Seat? smallBlindSeat,
        Seat? bigBlindSeat,
        Seat? buttonSeat,
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

        foreach (var player in players)
        {
            _players[player.Seat - 1] = player;
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
        var table = new Table(
            uid: uid,
            game: game,
            smallBlind: smallBlind,
            bigBlind: bigBlind,
            chipCost: chipCost,
            maxSeat: maxSeat,
            smallBlindSeat: null,
            bigBlindSeat: null,
            buttonSeat: null,
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

        var isWaitingForBigBlind = ShouldWaitForBigBlind();
        _players[seat - 1] = new Player(
            nickname: nickname,
            seat: seat,
            stack: stack,
            isDisconnected: false,
            isSittingOut: false,
            isWaitingForBigBlind: isWaitingForBigBlind
        );

        var @event = new PlayerSatDownEvent(
            Nickname: nickname,
            Seat: seat,
            Stack: stack,
            IsWaitingForBigBlind: isWaitingForBigBlind,
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

        var isWaitingForBigBlind = ShouldWaitForBigBlind();
        player.SitIn(
            isWaitingForBigBlind: isWaitingForBigBlind
        );

        var @event = new PlayerSatInEvent(
            Nickname: nickname,
            IsWaitingForBigBlind: isWaitingForBigBlind,
            OccuredAt: DateTime.Now
        );
        eventBus.Publish(@event);
    }

    public void StartHand(HandUid handUid, EventBus eventBus)
    {
        if (!HasEnoughPlayersForHand())
        {
            throw new InvalidOperationException("The table does not have enough players to start a hand");
        }

        var nextButtonSeat = GetNextButtonSeat();
        var nextSmallBlindSeat = GetNextSmallBlindSeat(nextButtonSeat);
        var nextBigBlindSeat = GetNextBigBlindSeat(nextSmallBlindSeat, nextButtonSeat);

        ButtonSeat = nextButtonSeat;
        SmallBlindSeat = nextSmallBlindSeat;
        BigBlindSeat = nextBigBlindSeat;
        HandUid = handUid;

        var bbPlayer = GetPlayerBySeat(nextBigBlindSeat);
        if (bbPlayer != null && bbPlayer.IsWaitingForBigBlind)
        {
            bbPlayer.StopWaitingForBigBlind();
        }

        var @event = new HandIsStartedEvent(
            HandUid: handUid,
            OccuredAt: DateTime.Now
        );
        eventBus.Publish(@event);
    }

    private bool HasEnoughPlayersForHand()
    {
        return ActivePlayers.Count() > 1;
    }

    private Seat GetNextButtonSeat()
    {
        var eligibleSeats = ActivePlayers
            .Where(p => !p.IsWaitingForBigBlind)
            .Select(p => p.Seat)
            .OrderBy(s => s)
            .ToHashSet();

        var nextSeat = ButtonSeat ?? MaxSeat; // For the first hand, start from the max seat

        do
        {
            nextSeat = GetNextSeat(nextSeat);
        } while (!eligibleSeats.Contains(nextSeat) || nextSeat == SmallBlindSeat || nextSeat == BigBlindSeat);

        return nextSeat;
    }

    private Seat GetNextSmallBlindSeat(Seat nextButtonSeat)
    {
        var eligibleSeats = ActivePlayers
            .Where(p => !p.IsWaitingForBigBlind)
            .Select(p => p.Seat)
            .OrderBy(s => s)
            .ToList();

        var smallBlindSeat = nextButtonSeat;

        do
        {
            smallBlindSeat = GetNextSeat(smallBlindSeat);
        } while (!eligibleSeats.Contains(smallBlindSeat) || smallBlindSeat == nextButtonSeat);

        return smallBlindSeat;
    }

    private Seat GetNextBigBlindSeat(Seat nextSmallBlindSeat, Seat nextButtonSeat)
    {
        var eligibleSeats = ActivePlayers
            .Select(p => p.Seat)
            .OrderBy(s => s)
            .ToList();

        var bigBlindSeat = nextSmallBlindSeat;

        do
        {
            bigBlindSeat = GetNextSeat(bigBlindSeat);
        } while (!eligibleSeats.Contains(bigBlindSeat) || bigBlindSeat == nextSmallBlindSeat || bigBlindSeat == nextButtonSeat);

        return bigBlindSeat;
    }

    private Seat GetNextSeat(Seat seat)
    {
        return seat == MaxSeat ? new Seat(1) : new Seat(seat + 1);
    }

    public IEnumerable<Participant> GetParticipants()
    {
        foreach (var player in ActivePlayers)
        {
            if (!player.IsWaitingForBigBlind)
            {
                yield return new Participant(
                    Nickname: player.Nickname,
                    Seat: player.Seat,
                    Stack: player.Stack
                );
            }
        }
    }

    private bool ShouldWaitForBigBlind()
    {
        return ActivePlayers.Count() > 1;
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
