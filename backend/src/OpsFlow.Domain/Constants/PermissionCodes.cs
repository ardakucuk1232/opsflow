namespace OpsFlow.Domain.Constants;

public static class PermissionCodes
{
    public const string CompanyManage = "company.manage";

    public const string UserView = "user.view";
    public const string UserInvite = "user.invite";
    public const string UserManage = "user.manage";
    public const string RoleManage = "role.manage";

    public const string ProjectViewAll = "project.view_all";
    public const string ProjectCreate = "project.create";
    public const string ProjectManage = "project.manage";

    public const string TaskCreate = "task.create";
    public const string TaskAssign = "task.assign";
    public const string TaskUpdate = "task.update";
    public const string TaskDelete = "task.delete";

    public const string CommentCreate = "comment.create";
    public const string AttachmentUpload = "attachment.upload";

    public const string ReportView = "report.view";
    public const string AuditLogView = "audit_log.view";
}