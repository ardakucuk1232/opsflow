namespace OpsFlow.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(ErrorCodes.NotFound, message)
    {
    }

    public NotFoundException(string code, string message) : base(code, message)
    {
    }

    public static NotFoundException For(string entityName, object key) =>
        new($"{entityName} with id '{key}' was not found.");
}
