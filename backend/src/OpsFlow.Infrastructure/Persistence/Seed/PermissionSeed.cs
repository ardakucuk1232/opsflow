using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Seed;

internal static class PermissionSeed
{
    private static readonly DateTimeOffset SeedDate = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static Permission Create(string id, string code, string group, string description) => new()
    {
        Id = Guid.Parse(id),
        Code = code,
        Group = group,
        Description = description,
        CreatedAt = SeedDate
    };

    public static Permission[] All => [
        Create("01990000-0000-7000-8000-000000000001", PermissionCodes.CompanyManage,    "Company",  "Manage company settings"),
        Create("01990000-0000-7000-8000-000000000002", PermissionCodes.UserView,         "Users",    "View users"),
        Create("01990000-0000-7000-8000-000000000003", PermissionCodes.UserInvite,       "Users",    "Invite new users"),
        Create("01990000-0000-7000-8000-000000000004", PermissionCodes.UserManage,       "Users",    "Edit, deactivate and delete users"),
        Create("01990000-0000-7000-8000-000000000005", PermissionCodes.RoleManage,       "Users",    "Manage roles and their permissions"),
        Create("01990000-0000-7000-8000-000000000006", PermissionCodes.ProjectViewAll,   "Projects", "View all projects, including those the user is not a member of"),
        Create("01990000-0000-7000-8000-000000000007", PermissionCodes.ProjectCreate,    "Projects", "Create projects"),
        Create("01990000-0000-7000-8000-000000000008", PermissionCodes.ProjectManage,    "Projects", "Edit projects and manage members"),
        Create("01990000-0000-7000-8000-000000000009", PermissionCodes.TaskCreate,       "Tasks",    "Create tasks"),
        Create("01990000-0000-7000-8000-00000000000a", PermissionCodes.TaskAssign,       "Tasks",    "Assign tasks to users"),
        Create("01990000-0000-7000-8000-00000000000b", PermissionCodes.TaskUpdate,       "Tasks",    "Update tasks and change their status"),
        Create("01990000-0000-7000-8000-00000000000c", PermissionCodes.TaskDelete,       "Tasks",    "Delete tasks"),
        Create("01990000-0000-7000-8000-00000000000d", PermissionCodes.CommentCreate,    "Tasks",    "Comment on tasks"),
        Create("01990000-0000-7000-8000-00000000000e", PermissionCodes.AttachmentUpload, "Tasks",    "Upload attachments"),
        Create("01990000-0000-7000-8000-00000000000f", PermissionCodes.ReportView,       "Insights", "View reports and dashboards"),
        Create("01990000-0000-7000-8000-000000000010", PermissionCodes.AuditLogView,     "Insights", "View audit logs"),
    ];
}