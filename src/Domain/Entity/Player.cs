using Domain.Exception;
using Domain.ValueObject;

namespace Domain.Entity;

public class Player : IEquatable<Player>
{
    public Nickname Nickname { get; }
    public Seat Seat { get; }
    public Chips Stack { get; private set; }
    public bool IsSittingOut { get; private set; }
    public bool IsWaitingForBigBlind { get; private set; }

    public bool IsActive => !IsSittingOut;

    public Player(
        Nickname nickname,
        Seat seat,
        Chips stack,
        bool isSittingOut = false,
        bool isWaitingForBigBlind = false
    )
    {
        Nickname = nickname;
        Seat = seat;
        Stack = stack;
        IsSittingOut = isSittingOut;
        IsWaitingForBigBlind = isWaitingForBigBlind;
    }

    public void SitOut()
    {
        if (IsSittingOut)
        {
            throw new PlayerSatOutException("The player is already sitting out");
        }

        IsSittingOut = true;
        IsWaitingForBigBlind = false;
    }

    public void SitIn(bool isWaitingForBigBlind)
    {
        if (!IsSittingOut)
        {
            throw new PlayerNotSatOutException("The player is not sitting out yet");
        }

        IsSittingOut = false;
        IsWaitingForBigBlind = isWaitingForBigBlind;
    }

    public void DebitChips(Chips amount)
    {
        Stack -= amount;
    }

    public void CreditChips(Chips amount)
    {
        Stack += amount;
    }

    public void StopWaitingForBigBlind()
    {
        if (!IsWaitingForBigBlind)
        {
            throw new PlayerNotWaitingForBigBlindException("The player is not waiting for big blind");
        }

        IsWaitingForBigBlind = false;
    }

    public bool Equals(Player? other)
    {
        return Nickname == other?.Nickname;
    }

    public override int GetHashCode()
    {
        return Nickname.GetHashCode();
    }

    public override string ToString()
        => $"{GetType().Name}: {Nickname}, {Seat}, {Stack}";
}
