namespace OpsFlow.Domain.Exceptions;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(ErrorCodes.Forbidden, message)
    {
    }

    public ForbiddenException(string code, string message) : base(code, message)
    {
    }
}
