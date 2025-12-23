using Domain.Event;
using Domain.ValueObject;

namespace Domain.Entity;

public class Table
{
    private const int MinPlayersForHand = 2;

    public readonly TableUid Uid;
    public readonly Game Game;
    public readonly Money ChipCost;
    public readonly Chips SmallBlind;
    public readonly Chips BigBlind;
    public readonly Seat MaxSeat;

    public Seat? SmallBlindSeat { get; private set; }
    public Seat? BigBlindSeat { get; private set; }
    public Seat? ButtonSeat { get; private set; }
    private HandUid? HandUid { get; set; }

    private readonly Player?[] _players;

    public IEnumerable<Player> Players => _players.OfType<Player>();
    private IEnumerable<Player> ActivePlayers => Players.Where(p => p.IsActive);
    private List<IEvent> _events;

    private Table(
        TableUid uid,
        Game game,
        Seat maxSeat,
        Chips smallBlind,
        Chips bigBlind,
        Money chipCost
    )
    {
        Uid = uid;
        Game = game;
        MaxSeat = maxSeat;
        SmallBlind = smallBlind;
        BigBlind = bigBlind;
        ChipCost = chipCost;

        SmallBlindSeat = null;
        BigBlindSeat = null;
        ButtonSeat = null;

        _players = new Player?[maxSeat];
        _events = [];
    }

    public static Table FromScratch(
        TableUid uid,
        Game game,
        Seat maxSeat,
        Chips smallBlind,
        Chips bigBlind,
        Money chipCost
    )
    {
        var table = new Table(
            uid: uid,
            game: game,
            smallBlind: smallBlind,
            bigBlind: bigBlind,
            chipCost: chipCost,
            maxSeat: maxSeat
        );

        var @event = new TableIsCreatedEvent
        {
            Game = game,
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            ChipCost = chipCost,
            MaxSeat = maxSeat,
            OccuredAt = DateTime.Now
        };
        table.AddEvent(@event);

        return table;
    }

    public static Table FromEvents(TableUid uid, List<IEvent> events)
    {
        if (events.Count == 0 || events[0] is not TableIsCreatedEvent)
        {
            throw new InvalidOperationException("The first event must be a TableIsCreatedEvent");
        }

        var createdEvent = (TableIsCreatedEvent)events[0];
        var table = FromScratch(
            uid: uid,
            game: createdEvent.Game,
            maxSeat: createdEvent.MaxSeat,
            smallBlind: createdEvent.SmallBlind,
            bigBlind: createdEvent.BigBlind,
            chipCost: createdEvent.ChipCost
        );

        foreach (var @event in events)
        {
            switch (@event)
            {
                case TableIsCreatedEvent:
                    break;
                case PlayerSatDownEvent e:
                    table.SitDown(e.Nickname, e.Seat, e.Stack);
                    break;
                case PlayerStoodUpEvent e:
                    table.StandUp(e.Nickname);
                    break;
                case PlayerSatOutEvent e:
                    table.SitOut(e.Nickname);
                    break;
                case PlayerSatInEvent e:
                    table.SitIn(e.Nickname);
                    break;
                case ButtonIsRotatedEvent:
                    table.RotateButton();
                    break;
                case HandIsStartedEvent e:
                    table.StartHand(e.HandUid);
                    break;
                case HandIsFinishedEvent e:
                    table.FinishHand(e.HandUid);
                    break;

                    // TODO: handle other events when they are added
            }
        }

        table.PullEvents();

        return table;
    }

    public void SitDown(Nickname nickname, Seat seat, Chips stack)
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

        var @event = new PlayerSatDownEvent
        {
            Nickname = nickname,
            Seat = seat,
            Stack = stack,
            OccuredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void StandUp(Nickname nickname)
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new InvalidOperationException("A player with the given nickname is not found at the table");
        }

        _players[player.Seat - 1] = null;

        var @event = new PlayerStoodUpEvent
        {
            Nickname = nickname,
            OccuredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void SitOut(Nickname nickname)
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new InvalidOperationException("A player with the given nickname is not found at the table");
        }

        player.SitOut();

        var @event = new PlayerSatOutEvent
        {
            Nickname = nickname,
            OccuredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void SitIn(Nickname nickname)
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

        var @event = new PlayerSatInEvent
        {
            Nickname = nickname,
            OccuredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void RotateButton()
    {
        if (IsHandInProgress())
        {
            throw new InvalidOperationException("The previous hand has not been finished yet");
        }

        if (!HasEnoughPlayersForHand())
        {
            throw new InvalidOperationException("Not enough players to rotate the button");
        }

        var nextButtonSeat = GetNextButtonSeat(ButtonSeat, SmallBlindSeat);
        var nextSmallBlindSeat = GetNextSmallBlindSeat(nextButtonSeat, BigBlindSeat);
        var nextBigBlindSeat = GetNextBigBlindSeat(nextSmallBlindSeat, nextButtonSeat);

        ButtonSeat = nextButtonSeat;
        SmallBlindSeat = nextSmallBlindSeat;
        BigBlindSeat = nextBigBlindSeat;

        var bbPlayer = GetPlayerBySeat((Seat)BigBlindSeat);
        if (bbPlayer is not null && bbPlayer.IsWaitingForBigBlind)
        {
            bbPlayer.StopWaitingForBigBlind();
        }

        var @event = new ButtonIsRotatedEvent
        {
            OccuredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void StartHand(HandUid handUid)
    {
        if (IsHandInProgress())
        {
            throw new InvalidOperationException("The previous hand has not been finished yet");
        }

        if (!HasEnoughPlayersForHand())
        {
            throw new InvalidOperationException("Not enough players to start a hand");
        }

        if (ButtonSeat == null || BigBlindSeat == null)
        {
            throw new InvalidOperationException("The button must be rotated before starting a hand");
        }

        HandUid = handUid;

        var @event = new HandIsStartedEvent
        {
            HandUid = handUid,
            OccuredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void FinishHand(HandUid handUid)
    {
        if (!IsHandInProgress())
        {
            throw new InvalidOperationException("The hand has not been started yet");
        }

        if (handUid != HandUid)
        {
            throw new InvalidOperationException("The hand does not match the current one");
        }

        HandUid = null;

        var @event = new HandIsFinishedEvent
        {
            HandUid = handUid,
            OccuredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public HandUid GetHandUid()
    {
        if (!IsHandInProgress())
        {
            throw new InvalidOperationException("The hand has not been started yet");
        }

        return (HandUid)HandUid!;
    }

    public bool IsHandInProgress()
    {
        return HandUid is not null;
    }

    public bool HasEnoughPlayersForHand()
    {
        return ActivePlayers.Count() >= MinPlayersForHand;
    }

    public IEnumerable<Participant> GetParticipants()
    {
        return ActivePlayers.Select(p => new Participant(
            Nickname: p.Nickname,
            Seat: p.Seat,
            Stack: p.Stack
        ));
    }

    private Seat GetNextButtonSeat(Seat? previousButtonSeat, Seat? previousSmallBlindSeat)
    {
        // Corner case: if a player on the small blind has left the table, we make this seat the Dead Button
        if (previousSmallBlindSeat is not null && SeatIsNotActive((Seat)previousSmallBlindSeat))
        {
            return (Seat)previousSmallBlindSeat;
        }

        var previousSeat = previousButtonSeat ?? MaxSeat; // For the first hand, start from the max seat
        return GetNextSeat(previousSeat, p => p.IsActive && !p.IsWaitingForBigBlind);
    }

    private Seat? GetNextSmallBlindSeat(Seat nextButtonSeat, Seat? previousBigBlindSeat)
    {
        // Corner case: if a player on the big blind has left the table, we play the next hand without the small blind
        if (previousBigBlindSeat is not null && SeatIsNotActive((Seat)previousBigBlindSeat))
        {
            return null;
        }

        if (ActivePlayers.Count(p => !p.IsWaitingForBigBlind) == 2)
        {
            if (ActivePlayers.Count() > 2)
            {
                var nextSeat = GetNextSeat(nextButtonSeat, p => p.IsActive);
                var nextPlayer = GetPlayerBySeat(nextSeat);
                if (nextPlayer is not null && nextPlayer.IsWaitingForBigBlind)
                {
                    // Corner case: if a new player joins the heads-up table and posts the big blind,
                    // we play the next hand without the small blind
                    return null;
                }
            }

            var buttonPlayer = GetPlayerBySeat(nextButtonSeat);
            if (buttonPlayer is not null && buttonPlayer.IsActive)
            {
                // Corner case: in heads-up, the button posts the small blind (except the Dead Button case)
                return nextButtonSeat;
            }
        }

        var previousSeat = nextButtonSeat;
        return GetNextSeat(previousSeat, p => p.IsActive && !p.IsWaitingForBigBlind);
    }

    private Seat GetNextBigBlindSeat(Seat? nextSmallBlindSeat, Seat nextButtonSeat)
    {
        // Corner case: if a player on the big blind has left the table, the next big blind is right after the button
        var previousSeat = nextSmallBlindSeat ?? nextButtonSeat;
        return GetNextSeat(previousSeat, p => p.IsActive);
    }

    private bool SeatIsNotActive(Seat seat)
    {
        var player = GetPlayerBySeat(seat);
        return player is null || !player.IsActive;
    }

    private Seat GetNextSeat(Seat previousSeat, Func<Player, bool>? predicate = null)
    {
        var seat = previousSeat;

        do
        {
            seat = seat == MaxSeat ? new Seat(1) : new Seat(seat + 1);

            if (seat == previousSeat)
            {
                throw new InvalidOperationException("No eligible seats");
            }

            var player = GetPlayerBySeat(seat);

            if (player is not null && (predicate is null || predicate(player)))
            {
                return seat;
            }
        } while (true);
    }

    private bool ShouldWaitForBigBlind()
    {
        return IsHandInProgress();
    }

    private Player? GetPlayerByNickname(Nickname nickname)
    {
        return Players.FirstOrDefault(p => p.Nickname == nickname);
    }

    private Player? GetPlayerBySeat(Seat seat)
    {
        return _players[seat - 1];
    }

    # region Events

    public List<IEvent> PullEvents()
    {
        var events = _events.ToList();
        _events.Clear();

        return events;
    }

    private void AddEvent(IEvent @event)
    {
        _events.Add(@event);
    }

    # endregion
}
