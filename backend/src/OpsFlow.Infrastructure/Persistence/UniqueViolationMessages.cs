namespace OpsFlow.Infrastructure.Persistence;

internal static class UniqueViolationMessages
{
    private const string DefaultMessage = "A record with the same value already exists.";

    private static readonly Dictionary<string, string> MessagesByConstraint = new(StringComparer.Ordinal)
    {
        ["IX_Users_Email"] = "An account with this email address already exists."
    };

    public static string For(string? constraintName)
    {
        if (constraintName is not null && MessagesByConstraint.TryGetValue(constraintName, out var message))
        {
            return message;
        }

        return DefaultMessage;
    }
}
