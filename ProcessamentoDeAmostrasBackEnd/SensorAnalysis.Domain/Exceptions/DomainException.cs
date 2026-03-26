namespace SensorAnalysis.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class InvalidJobOperationException : DomainException
{
    public InvalidJobOperationException(string message) : base(message) { }
}
