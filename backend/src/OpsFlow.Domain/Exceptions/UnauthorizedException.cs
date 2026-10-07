namespace OpsFlow.Domain.Exceptions;

public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(ErrorCodes.Unauthorized, message)
    {
    }

    public UnauthorizedException(string code, string message) : base(code, message)
    {
    }
}
