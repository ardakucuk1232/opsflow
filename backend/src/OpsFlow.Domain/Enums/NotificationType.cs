namespace OpsFlow.Domain.Enums;

public enum NotificationType
{
    TaskAssigned = 1,
    TaskStatusChanged = 2,
    TaskCommentAdded = 3,
    TaskDueSoon = 4,
    ProjectMemberAdded = 5,
    MentionedInComment = 6
}