namespace OpsFlow.Domain.Exceptions;

public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(ErrorCodes.BusinessRuleViolation, message)
    {
    }

    public BusinessRuleException(string code, string message) : base(code, message)
    {
    }
}
