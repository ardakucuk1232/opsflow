namespace OpsFlow.Domain.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(ErrorCodes.Conflict, message)
    {
    }

    public ConflictException(string code, string message) : base(code, message)
    {
    }

    public ConflictException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }
}
