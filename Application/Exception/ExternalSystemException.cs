namespace Application.Exception;

public abstract class ExternalSystemException(string message, System.Exception? innerException = null)
    : System.Exception(message, innerException);

public class ExternalSystemUnavailableException(string message, System.Exception? innerException = null)
    : ExternalSystemException(message, innerException);

public class ExternalSystemTimeoutException(string message, System.Exception? innerException = null)
    : ExternalSystemException(message, innerException);

public class ExternalSystemErrorException(string message, System.Exception? innerException = null)
    : ExternalSystemException(message, innerException);

public class ExternalSystemContractViolatedException(string message, System.Exception? innerException = null)
    : ExternalSystemException(message, innerException);
