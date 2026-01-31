namespace Domain.Exception;

public abstract class InvariantViolatedException(string message) : System.Exception(message);

public class InsufficientMoneyException(string message) : InvariantViolatedException(message);

public class InsufficientChipsException(string message) : InvariantViolatedException(message);

public class InvalidTableStateException(string message) : InvariantViolatedException(message);
