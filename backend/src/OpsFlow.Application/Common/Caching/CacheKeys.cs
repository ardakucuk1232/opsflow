namespace OpsFlow.Application.Common.Caching;

public static class CacheKeys
{
    public static readonly TimeSpan PermissionsLifetime = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan BoardLifetime = TimeSpan.FromMinutes(10);

    public static string Permissions(Guid userId) => $"permissions:{userId:N}";

    public static string Board(Guid companyId, Guid projectId) => $"board:{companyId:N}:{projectId:N}";
}
