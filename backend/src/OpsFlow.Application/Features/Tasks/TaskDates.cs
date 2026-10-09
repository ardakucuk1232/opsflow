namespace OpsFlow.Application.Features.Tasks;

internal static class TaskDates
{
    public static DateTimeOffset? ToStored(DateOnly? date) =>
        date is DateOnly value ? new DateTimeOffset(value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    public static DateOnly? FromStored(DateTimeOffset? value) =>
        value is DateTimeOffset stored ? DateOnly.FromDateTime(stored.UtcDateTime) : null;
}
