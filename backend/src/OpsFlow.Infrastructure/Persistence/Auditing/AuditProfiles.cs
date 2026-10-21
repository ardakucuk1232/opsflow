using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Persistence.Seed;

namespace OpsFlow.Infrastructure.Persistence.Auditing;

internal sealed record AuditProfile(
    Func<object, DbContext, string?> Label,
    IReadOnlySet<string> Ignored,
    IReadOnlySet<string> Masked);

internal static class AuditProfiles
{
    public static readonly IReadOnlySet<string> AlwaysIgnored = new HashSet<string>(StringComparer.Ordinal)
    {
        "Id", "CompanyId", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
    };

    private static readonly IReadOnlySet<string> Nothing = new HashSet<string>();

    private static readonly IReadOnlyDictionary<Guid, string> PermissionCodes =
        PermissionSeed.All.ToDictionary(p => p.Id, p => p.Code);

    private static readonly Dictionary<Type, AuditProfile> Profiles = new()
    {
        [typeof(Company)] = Profile<Company>(c => c.Name),
        [typeof(User)] = Profile<User>(u => u.Email, ignored: ["LastLoginAt"], masked: ["PasswordHash"]),
        [typeof(Role)] = Profile<Role>(r => r.Name),
        [typeof(RolePermission)] = Profile<RolePermission>((rp, db) => Join(
            (rp.Role ?? Tracked<Role>(db, rp.RoleId))?.Name,
            PermissionCode(rp.PermissionId))),
        [typeof(UserRole)] = Profile<UserRole>((ur, db) => Join(
            (ur.User ?? Tracked<User>(db, ur.UserId))?.Email,
            (ur.Role ?? Tracked<Role>(db, ur.RoleId))?.Name), ignored: ["AssignedAt"]),
        [typeof(Project)] = Profile<Project>(p => $"{p.Key} · {p.Name}", ignored: ["LastTaskNumber"]),
        [typeof(ProjectMember)] = Profile<ProjectMember>((m, db) => Join(
            (m.Project ?? Tracked<Project>(db, m.ProjectId))?.Key,
            (m.User ?? Tracked<User>(db, m.UserId))?.Email), ignored: ["JoinedAt"]),
        [typeof(TaskItem)] = Profile<TaskItem>(t => t.Title, ignored: ["BoardOrder"]),
        [typeof(TaskComment)] = Profile<TaskComment>(c => c.Body),
        [typeof(Attachment)] = Profile<Attachment>(a => a.FileName, ignored: ["StoredFileName", "StoragePath"])
    };

    public static bool TryGet(Type type, out AuditProfile profile) => Profiles.TryGetValue(type, out profile!);

    public static string? PermissionCode(Guid permissionId) =>
        PermissionCodes.TryGetValue(permissionId, out var code) ? code : null;

    private static AuditProfile Profile<T>(Func<T, string?> label, string[]? ignored = null, string[]? masked = null) =>
        Profile<T>((entity, _) => label(entity), ignored, masked);

    private static AuditProfile Profile<T>(Func<T, DbContext, string?> label, string[]? ignored = null, string[]? masked = null) =>
        new((entity, db) => label((T)entity, db), ignored?.ToHashSet(StringComparer.Ordinal) ?? Nothing, masked?.ToHashSet(StringComparer.Ordinal) ?? Nothing);

    private static T? Tracked<T>(DbContext db, Guid id) where T : BaseEntity =>
        db.ChangeTracker.Entries<T>().Select(e => e.Entity).FirstOrDefault(e => e.Id == id);

    private static string? Join(string? left, string? right) => (left, right) switch
    {
        (null, null) => null,
        (null, _) => right,
        (_, null) => left,
        _ => $"{left} → {right}"
    };
}
