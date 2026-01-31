namespace Application.Exception;

public abstract class NotFoundException : System.Exception
{
    protected NotFoundException(string message) : base(message) { }

    protected NotFoundException(string message, System.Exception innerException) : base(message, innerException) { }
}

public class TableNotFoundException : NotFoundException
{
    public TableNotFoundException(string message) : base(message) { }

    public TableNotFoundException(string message, System.Exception innerException) : base(message, innerException) { }
}
