namespace Domain.Exception;

public abstract class DomainException(string message) : System.Exception(message);

public class InvalidTableConfigurationException(string message) : DomainException(message);

public class SeatNotFoundException(string message) : DomainException(message);

public class SeatOccupiedException(string message) : DomainException(message);

public class PlayerSatDownException(string message) : DomainException(message);

public class PlayerSatOutException(string message) : DomainException(message);

public class PlayerNotSatOutException(string message) : DomainException(message);

public class PlayerNotWaitingForBigBlindException(string message) : DomainException(message);

public class PlayerNotFoundException(string message) : DomainException(message);
