namespace OpsFlow.Application.Common.Persistence;

public static class SqlText
{
    public const string EscapeCharacter = "\\";

    public static string Lower(string value) =>
        throw new NotSupportedException($"{nameof(SqlText)}.{nameof(Lower)} can only be used inside a database query.");

    public static string ContainsPattern(string term) =>
        "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
