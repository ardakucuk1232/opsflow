namespace OpsFlow.Domain.Constants;

public static class SystemRolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> Map = new Dictionary<string, IReadOnlyCollection<string>>
    {
        [SystemRoles.Admin] = [
            PermissionCodes.CompanyManage,
            PermissionCodes.UserView,
            PermissionCodes.UserInvite,
            PermissionCodes.UserManage,
            PermissionCodes.RoleManage,
            PermissionCodes.ProjectViewAll,
            PermissionCodes.ProjectCreate,
            PermissionCodes.ProjectManage,
            PermissionCodes.TaskCreate,
            PermissionCodes.TaskAssign,
            PermissionCodes.TaskUpdate,
            PermissionCodes.TaskDelete,
            PermissionCodes.CommentCreate,
            PermissionCodes.AttachmentUpload,
            PermissionCodes.ReportView,
            PermissionCodes.AuditLogView
        ],

        [SystemRoles.Manager] =
        [
            PermissionCodes.UserView,
            PermissionCodes.ProjectViewAll,
            PermissionCodes.ProjectCreate,
            PermissionCodes.ProjectManage,
            PermissionCodes.TaskCreate,
            PermissionCodes.TaskAssign,
            PermissionCodes.TaskUpdate,
            PermissionCodes.TaskDelete,
            PermissionCodes.CommentCreate,
            PermissionCodes.AttachmentUpload,
            PermissionCodes.ReportView
        ],

        [SystemRoles.Employee] =
        [
            PermissionCodes.UserView,
            PermissionCodes.TaskUpdate,
            PermissionCodes.CommentCreate,
            PermissionCodes.AttachmentUpload
        ]
    };
}