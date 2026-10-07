using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Infrastructure.Persistence;

internal static class UniqueViolationMessages
{
    private static readonly (string Code, string Message) Default =
        (ErrorCodes.Conflict, "A record with the same value already exists.");

    private static readonly Dictionary<string, (string Code, string Message)> ByConstraint = new(StringComparer.Ordinal)
    {
        ["IX_Users_Email"] = (ErrorCodes.Auth.EmailAlreadyInUse, "An account with this email address already exists.")
    };

    public static (string Code, string Message) For(string? constraintName)
    {
        if (constraintName is not null && ByConstraint.TryGetValue(constraintName, out var entry))
        {
            return entry;
        }

        return Default;
    }
}
