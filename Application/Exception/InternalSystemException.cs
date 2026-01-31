namespace Application.Exception;

public abstract class InternalSystemException(string message, System.Exception? innerException = null)
    : System.Exception(message, innerException);

public class InternalSystemMisconfiguredException(string message, System.Exception? innerException = null)
    : InternalSystemException(message, innerException);
