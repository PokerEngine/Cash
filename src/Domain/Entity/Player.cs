using Domain.ValueObject;

namespace Domain.Entity;

public class Player : IEquatable<Player>
{
    public Nickname Nickname { get; }
    public Seat Seat { get; }
    public Chips Stack { get; private set; }
    public bool IsDisconnected { get; private set; }
    public bool IsSittingOut { get; private set; }
    public bool IsWaitingForBigBlind { get; private set; }

    public bool IsActive => !IsDisconnected && !IsSittingOut && !!Stack;

    public Player(
        Nickname nickname,
        Seat seat,
        Chips stack,
        bool isDisconnected = false,
        bool isSittingOut = false,
        bool isWaitingForBigBlind = false
    )
    {
        Nickname = nickname;
        Seat = seat;
        Stack = stack;
        IsDisconnected = isDisconnected;
        IsSittingOut = isSittingOut;
        IsWaitingForBigBlind = isWaitingForBigBlind;
    }

    public void Connect()
    {
        if (!IsDisconnected)
        {
            throw new InvalidOperationException("The player has already connected");
        }

        IsDisconnected = false;
    }

    public void Disconnect()
    {
        if (IsDisconnected)
        {
            throw new InvalidOperationException("The player has not connected yet");
        }

        IsDisconnected = true;
    }

    public void SitOut()
    {
        if (IsDisconnected)
        {
            throw new InvalidOperationException("The player has not connected yet");
        }

        if (IsSittingOut)
        {
            throw new InvalidOperationException("The player is already sitting out");
        }

        IsSittingOut = true;
        IsWaitingForBigBlind = false;
    }

    public void SitIn()
    {
        if (IsDisconnected)
        {
            throw new InvalidOperationException("The player has not connected yet");
        }

        if (!IsSittingOut)
        {
            throw new InvalidOperationException("The player is not sitting out yet");
        }

        IsSittingOut = false;
        IsWaitingForBigBlind = true;
    }

    public void StopWaitingForBigBlind()
    {
        if (!IsWaitingForBigBlind)
        {
            throw new InvalidOperationException("The player is not waiting for big blind");
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
        => $"{Nickname}, {Seat}, {Stack}";
}
