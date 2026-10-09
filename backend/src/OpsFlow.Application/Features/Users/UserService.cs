using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Common.Security;
using OpsFlow.Application.Features.Auth.Emails;
using OpsFlow.Application.Features.Auth.Tokens;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Users;

public sealed class UserService : IUserService
{
    private static readonly Expression<Func<User, UserSummaryDto>> ToSummary = u => new UserSummaryDto(
        u.Id,
        u.Email,
        u.FirstName,
        u.LastName,
        u.IsActive,
        u.IsEmailVerified,
        u.PasswordHash == "",
        u.UserRoles
            .OrderBy(ur => ur.Role.Name)
            .Select(ur => new RoleReferenceDto(ur.Role.Id, ur.Role.Name))
            .ToList(),
        u.LastLoginAt,
        u.CreatedAt);

    private readonly IOpsFlowDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly PrivilegeGuard _privilegeGuard;
    private readonly AccountMailer _mailer;
    private readonly UserTokenManager _tokens;
    private readonly IValidator<UserListQuery> _listValidator;
    private readonly IValidator<InviteUserRequest> _inviteValidator;
    private readonly IValidator<UpdateUserRolesRequest> _updateRolesValidator;
    private readonly TimeProvider _timeProvider;

    public UserService(
        IOpsFlowDbContext db,
        ITenantContext tenantContext,
        PrivilegeGuard privilegeGuard,
        AccountMailer mailer,
        UserTokenManager tokens,
        IValidator<UserListQuery> listValidator,
        IValidator<InviteUserRequest> inviteValidator,
        IValidator<UpdateUserRolesRequest> updateRolesValidator,
        TimeProvider timeProvider)
    {
        _db = db;
        _tenantContext = tenantContext;
        _privilegeGuard = privilegeGuard;
        _mailer = mailer;
        _tokens = tokens;
        _listValidator = listValidator;
        _inviteValidator = inviteValidator;
        _updateRolesValidator = updateRolesValidator;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<UserSummaryDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        await _listValidator.ValidateAndThrowAsync(query, cancellationToken);

        var users = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();

            users = users.Where(u =>
                u.Email.ToLower().Contains(term)
                || (u.FirstName + " " + u.LastName).ToLower().Contains(term));
        }

        users = query.Status switch
        {
            UserStatus.Active => users.Where(u => u.IsActive && u.PasswordHash != ""),
            UserStatus.Invited => users.Where(u => u.IsActive && u.PasswordHash == ""),
            UserStatus.Inactive => users.Where(u => !u.IsActive),
            _ => users
        };

        if (query.RoleId is Guid roleId)
        {
            users = users.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId));
        }

        return await users
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.Id)
            .Select(ToSummary)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<UserSummaryDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(ToSummary)
            .SingleOrDefaultAsync(cancellationToken);

        return user ?? throw new NotFoundException("The user was not found.");
    }

    public async Task<UserSummaryDto> InviteAsync(InviteUserRequest request, CancellationToken cancellationToken)
    {
        await _inviteValidator.ValidateAndThrowAsync(request, cancellationToken);

        var inviter = await LoadCurrentUserAsync(cancellationToken);
        EnsureEmailVerified(inviter);

        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException(
                ErrorCodes.Auth.EmailAlreadyInUse,
                "An account with this email address already exists.");
        }

        var roles = await LoadRolesAsync(request.RoleIds, cancellationToken);
        await _privilegeGuard.EnsureHoldsAllAsync(PermissionsOf(roles), cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var user = new User
        {
            CompanyId = inviter.CompanyId,
            Email = email,
            PasswordHash = string.Empty,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                CompanyId = inviter.CompanyId,
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = now
            });
        }

        _db.Users.Add(user);

        var invitation = _mailer.PrepareInvitation(user, inviter, inviter.Company.Name, now);

        await _db.SaveChangesAsync(cancellationToken);

        _mailer.Send(invitation);

        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task ResendInvitationAsync(Guid id, CancellationToken cancellationToken)
    {
        var inviter = await LoadCurrentUserAsync(cancellationToken);
        EnsureEmailVerified(inviter);

        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("The user was not found.");

        if (!string.IsNullOrEmpty(user.PasswordHash) || !user.IsActive)
        {
            throw new BusinessRuleException(
                ErrorCodes.Users.InvitationAlreadyAccepted,
                "This user has no pending invitation.");
        }

        var now = _timeProvider.GetUtcNow();

        var recentlySent = await _tokens.WasIssuedSinceAsync(
            user.Id, UserTokenType.Invitation, now - _mailer.Cooldown, cancellationToken);

        if (recentlySent)
        {
            return;
        }

        await _tokens.InvalidateAsync(user.Id, UserTokenType.Invitation, now, cancellationToken);

        var invitation = _mailer.PrepareInvitation(user, inviter, inviter.Company.Name, now);

        await _db.SaveChangesAsync(cancellationToken);

        _mailer.Send(invitation);
    }

    public async Task<UserSummaryDto> UpdateRolesAsync(
        Guid id,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        await _updateRolesValidator.ValidateAndThrowAsync(request, cancellationToken);

        EnsureNotSelf(id);

        var user = await LoadUserWithRolesAsync(id, cancellationToken);
        var newRoles = await LoadRolesAsync(request.RoleIds, cancellationToken);
        var currentRoles = user.UserRoles.Select(ur => ur.Role).ToList();

        await _privilegeGuard.EnsureHoldsAllAsync(
            PermissionsOf(currentRoles).Concat(PermissionsOf(newRoles)),
            cancellationToken);

        if (user.IsActive && currentRoles.Any(IsAdminRole) && !newRoles.Any(IsAdminRole))
        {
            await EnsureAnotherAdminExistsAsync(user.Id, cancellationToken);
        }

        var newRoleIds = newRoles.Select(r => r.Id).ToHashSet();

        foreach (var userRole in user.UserRoles.Where(ur => !newRoleIds.Contains(ur.RoleId)).ToList())
        {
            user.UserRoles.Remove(userRole);
        }

        var now = _timeProvider.GetUtcNow();

        foreach (var role in newRoles.Where(r => user.UserRoles.All(ur => ur.RoleId != r.Id)))
        {
            user.UserRoles.Add(new UserRole
            {
                CompanyId = user.CompanyId,
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, string? ipAddress, CancellationToken cancellationToken)
    {
        EnsureNotSelf(id);

        var user = await LoadUserWithRolesAsync(id, cancellationToken);
        var roles = user.UserRoles.Select(ur => ur.Role).ToList();

        await _privilegeGuard.EnsureHoldsAllAsync(PermissionsOf(roles), cancellationToken);

        if (!user.IsActive)
        {
            return;
        }

        if (roles.Any(IsAdminRole))
        {
            await EnsureAnotherAdminExistsAsync(user.Id, cancellationToken);
        }

        var now = _timeProvider.GetUtcNow();

        user.IsActive = false;

        await _db.SaveChangesAsync(cancellationToken);

        await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, (DateTimeOffset?)now)
                    .SetProperty(t => t.RevokedByIp, ipAddress)
                    .SetProperty(t => t.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureNotSelf(id);

        var user = await LoadUserWithRolesAsync(id, cancellationToken);

        await _privilegeGuard.EnsureHoldsAllAsync(
            PermissionsOf(user.UserRoles.Select(ur => ur.Role)),
            cancellationToken);

        if (user.IsActive)
        {
            return;
        }

        user.IsActive = true;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsAdminRole(Role role) => role.IsSystemRole && role.Name == SystemRoles.Admin;

    private static IEnumerable<string> PermissionsOf(IEnumerable<Role> roles) => roles
        .SelectMany(r => r.RolePermissions)
        .Select(rp => rp.Permission.Code)
        .Distinct(StringComparer.Ordinal);

    private static void EnsureEmailVerified(User user)
    {
        if (!user.IsEmailVerified)
        {
            throw new BusinessRuleException(
                ErrorCodes.Users.EmailNotVerified,
                "Verify your email address before inviting users.");
        }
    }

    private void EnsureNotSelf(Guid id)
    {
        if (_tenantContext.UserId == id)
        {
            throw new BusinessRuleException(
                ErrorCodes.Users.CannotModifySelf,
                "You cannot change your own roles or status.");
        }
    }

    private async Task EnsureAnotherAdminExistsAsync(Guid excludedUserId, CancellationToken cancellationToken)
    {
        var anotherAdminExists = await _db.UserRoles.AnyAsync(
            ur => ur.UserId != excludedUserId
                && ur.Role.IsSystemRole
                && ur.Role.Name == SystemRoles.Admin
                && ur.User.IsActive
                && ur.User.PasswordHash != "",
            cancellationToken);

        if (!anotherAdminExists)
        {
            throw new BusinessRuleException(
                ErrorCodes.Users.LastAdmin,
                "The company must keep at least one active admin.");
        }
    }

    private async Task<User> LoadCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (_tenantContext.UserId is not Guid userId)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        return await _db.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("The user associated with this token is no longer available.");
    }

    private async Task<User> LoadUserWithRolesAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("The user was not found.");
    }

    private async Task<List<Role>> LoadRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds.Distinct().ToList();

        var roles = await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

        if (roles.Count != ids.Count)
        {
            throw new BusinessRuleException(ErrorCodes.Users.InvalidRoles, "One or more roles do not exist.");
        }

        return roles;
    }
}
