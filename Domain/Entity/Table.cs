using Domain.Event;
using Domain.Exception;
using Domain.ValueObject;

namespace Domain.Entity;

public class Table
{
    private const int MinPlayersForHand = 2;

    public readonly TableUid Uid;
    public readonly Rules Rules;
    public Positions? Positions { get; private set; }
    private HandUid? CurrentHandUid { get; set; }

    private readonly Player?[] _players;

    public IEnumerable<Player> Players => _players.OfType<Player>();
    public IEnumerable<Player> ActivePlayers => Players.Where(p => p.IsActive);

    private readonly List<IEvent> _events;

    private Table(
        TableUid uid,
        Rules rules
    )
    {
        Uid = uid;
        Rules = rules;

        Positions = null;

        _players = new Player?[rules.MaxSeat];
        _events = [];
    }

    public static Table FromScratch(
        TableUid uid,
        Rules rules
    )
    {
        var table = new Table(
            uid: uid,
            rules: rules
        );

        var @event = new TableCreatedEvent
        {
            Rules = rules,
            OccurredAt = DateTime.Now
        };
        table.AddEvent(@event);

        return table;
    }

    public static Table FromEvents(TableUid uid, List<IEvent> events)
    {
        if (events.Count == 0 || events[0] is not TableCreatedEvent)
        {
            throw new InvalidTableStateException("The first event must be a TableCreatedEvent");
        }

        var createdEvent = (TableCreatedEvent)events[0];
        var table = new Table(
            uid: uid,
            rules: createdEvent.Rules
        );

        foreach (var @event in events)
        {
            switch (@event)
            {
                case TableCreatedEvent:
                    break;
                case PlayerSatDownEvent e:
                    table.SitPlayerDown(e.Nickname, e.Seat, e.Stack);
                    break;
                case PlayerStoodUpEvent e:
                    table.StandPlayerUp(e.Nickname);
                    break;
                case PlayerSatOutEvent e:
                    table.SitPlayerOut(e.Nickname);
                    break;
                case PlayerSatInEvent e:
                    table.SitPlayerIn(e.Nickname);
                    break;
                case PlayerChipsDebitedEvent e:
                    table.DebitPlayerChips(e.Nickname, e.Amount);
                    break;
                case PlayerChipsCreditedEvent e:
                    table.CreditPlayerChips(e.Nickname, e.Amount);
                    break;
                case ButtonRotatedEvent:
                    table.RotateButton();
                    break;
                case CurrentHandStartedEvent e:
                    table.StartCurrentHand(e.HandUid);
                    break;
                case CurrentHandFinishedEvent e:
                    table.FinishCurrentHand(e.HandUid);
                    break;
            }
        }

        table.PullEvents();

        return table;
    }

    public void SitPlayerDown(Nickname nickname, Seat seat, Chips stack)
    {
        if (seat > Rules.MaxSeat)
        {
            throw new SeatNotFoundException($"The table supports seats till {Rules.MaxSeat}");
        }

        var player = GetPlayerBySeat(seat);
        if (player is not null)
        {
            throw new SeatOccupiedException("The seat has already been occupied at the table");
        }

        player = GetPlayerByNickname(nickname);
        if (player is not null)
        {
            throw new PlayerSatDownException("The player has already sat down at the table");
        }

        var isWaitingForBigBlind = ShouldWaitForBigBlind();
        _players[seat - 1] = new Player(
            nickname: nickname,
            seat: seat,
            stack: stack,
            isSittingOut: false,
            isWaitingForBigBlind: isWaitingForBigBlind
        );

        var @event = new PlayerSatDownEvent
        {
            Nickname = nickname,
            Seat = seat,
            Stack = stack,
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void StandPlayerUp(Nickname nickname)
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new PlayerNotFoundException("The player is not found at the table");
        }

        _players[player.Seat - 1] = null;

        var @event = new PlayerStoodUpEvent
        {
            Nickname = nickname,
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void SitPlayerOut(Nickname nickname)
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new PlayerNotFoundException("The player is not found at the table");
        }

        player.SitOut();

        var @event = new PlayerSatOutEvent
        {
            Nickname = nickname,
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void SitPlayerIn(Nickname nickname)
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new PlayerNotFoundException("The player is not found at the table");
        }

        var isWaitingForBigBlind = ShouldWaitForBigBlind();
        player.SitIn(isWaitingForBigBlind);

        var @event = new PlayerSatInEvent
        {
            Nickname = nickname,
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void DebitPlayerChips(Nickname nickname, Chips amount)
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new PlayerNotFoundException("The player is not found at the table");
        }

        player.DebitChips(amount);

        var @event = new PlayerChipsDebitedEvent
        {
            Nickname = nickname,
            Amount = amount,
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void CreditPlayerChips(Nickname nickname, Chips amount)
    {
        var player = GetPlayerByNickname(nickname);
        if (player is null)
        {
            throw new PlayerNotFoundException("The player is not found at the table");
        }

        player.CreditChips(amount);

        var @event = new PlayerChipsCreditedEvent
        {
            Nickname = nickname,
            Amount = amount,
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void RotateButton()
    {
        if (IsHandInProgress())
        {
            throw new InvalidTableStateException("The previous hand has not been finished yet");
        }

        if (!HasEnoughPlayersForHand())
        {
            throw new InvalidTableStateException("Not enough players to rotate the button");
        }

        var nextButtonSeat = GetNextButtonSeat(Positions?.ButtonSeat, Positions?.SmallBlindSeat);
        var nextSmallBlindSeat = GetNextSmallBlindSeat(nextButtonSeat, Positions?.BigBlindSeat);
        var nextBigBlindSeat = GetNextBigBlindSeat(nextSmallBlindSeat, nextButtonSeat);

        Positions = new Positions
        {
            SmallBlindSeat = nextSmallBlindSeat,
            BigBlindSeat = nextBigBlindSeat,
            ButtonSeat = nextButtonSeat
        };

        var bbPlayer = GetPlayerBySeat(Positions.BigBlindSeat);
        if (bbPlayer is not null && bbPlayer.IsWaitingForBigBlind)
        {
            bbPlayer.StopWaitingForBigBlind();
        }

        var @event = new ButtonRotatedEvent
        {
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void StartCurrentHand(HandUid handUid)
    {
        if (IsHandInProgress())
        {
            throw new InvalidTableStateException("The previous hand has not been finished yet");
        }

        CurrentHandUid = handUid;

        var @event = new CurrentHandStartedEvent
        {
            HandUid = handUid,
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public void FinishCurrentHand(HandUid handUid)
    {
        if (!IsHandInProgress())
        {
            throw new InvalidTableStateException("The current hand has not been started yet");
        }

        if (handUid != CurrentHandUid)
        {
            throw new InvalidTableStateException("The hand does not match the current one");
        }

        CurrentHandUid = null;

        var @event = new CurrentHandFinishedEvent
        {
            HandUid = handUid,
            OccurredAt = DateTime.Now
        };
        AddEvent(@event);
    }

    public HandUid GetCurrentHandUid()
    {
        if (!IsHandInProgress())
        {
            throw new InvalidTableStateException("The current hand has not been set yet");
        }

        return (HandUid)CurrentHandUid!;
    }

    public bool IsHandInProgress()
    {
        return CurrentHandUid is not null;
    }

    public bool HasEnoughPlayersForHand()
    {
        return ActivePlayers.Count() >= MinPlayersForHand;
    }

    private Seat GetNextButtonSeat(Seat? previousButtonSeat, Seat? previousSmallBlindSeat)
    {
        // Corner case: if a player on the small blind has left the table, we make this seat the Dead Button
        if (previousSmallBlindSeat is not null && SeatIsNotActive((Seat)previousSmallBlindSeat))
        {
            return (Seat)previousSmallBlindSeat;
        }

        var previousSeat = previousButtonSeat ?? Rules.MaxSeat; // For the first hand, start from the max seat
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
            seat = seat == Rules.MaxSeat ? new Seat(1) : new Seat(seat + 1);

            if (seat == previousSeat)
            {
                throw new InvalidTableStateException("No eligible seats at the table");
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
