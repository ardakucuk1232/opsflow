namespace OpsFlow.Application.Features.Notifications;

public static class NotificationLinks
{
    public const string TaskEntity = "Task";
    public const string ProjectEntity = "Project";

    public static string ForTask(Guid projectId, Guid taskId) => $"/projects/{projectId}?task={taskId}";

    public static string ForProject(Guid projectId) => $"/projects/{projectId}";
}
