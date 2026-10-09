using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Options;
using OpsFlow.Application.Common.Text;
using OpsFlow.Application.Features.Auth.Emails;
using OpsFlow.Application.Features.Auth.Tokens;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";
    private const string InvalidRefreshTokenMessage = "Invalid or expired refresh token.";
    private const string InvalidInvitationMessage = "The invitation is invalid or has expired.";

    // Kullanıcı bulunamadığında BCrypt'i boşuna çalıştırmak için kullanılan hash.
    // static: tüm AuthService örnekleri paylaşır, uygulama ömrü boyunca bir kez üretilir.
    private static string? _dummyPasswordHash;

    private readonly IOpsFlowDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ISecureTokenService _secureTokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly AccountMailer _accountMailer;
    private readonly UserTokenManager _userTokens;
    private readonly IValidator<InvitationTokenRequest> _invitationTokenValidator;
    private readonly IValidator<AcceptInvitationRequest> _acceptInvitationValidator;

    public AuthService(
        IOpsFlowDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ISecureTokenService secureTokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider,
        ILogger<AuthService> logger,
        ITenantContext tenantContext,
        AccountMailer accountMailer,
        UserTokenManager userTokens,
        IValidator<InvitationTokenRequest> invitationTokenValidator,
        IValidator<AcceptInvitationRequest> acceptInvitationValidator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _secureTokenService = secureTokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
        _logger = logger;
        _tenantContext = tenantContext;
        _accountMailer = accountMailer;
        _userTokens = userTokens;
        _invitationTokenValidator = invitationTokenValidator;
        _acceptInvitationValidator = acceptInvitationValidator;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);

        var emailTaken = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException(
                ErrorCodes.Auth.EmailAlreadyInUse,
                "An account with this email address already exists.");
        }

        var now = _timeProvider.GetUtcNow();

        var company = new Company
        {
            Name = request.CompanyName.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.CompanyName, cancellationToken)
        };

        var permissionsByCode = await _db.Permissions
            .ToDictionaryAsync(p => p.Code, cancellationToken);

        var roles = CreateSystemRoles(company.Id, permissionsByCode);
        var adminRole = roles.Single(r => r.Name == SystemRoles.Admin);

        var user = new User
        {
            CompanyId = company.Id,
            Company = company,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            LastLoginAt = now
        };

        user.UserRoles.Add(new UserRole
        {
            CompanyId = company.Id,
            UserId = user.Id,
            RoleId = adminRole.Id,
            AssignedAt = now
        });

        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(user, ipAddress, now);

        _db.Companies.Add(company);
        _db.Roles.AddRange(roles);
        _db.Users.Add(user);
        _db.RefreshTokens.Add(refreshTokenEntity);

        var verificationEmail = _accountMailer.PrepareEmailVerification(user, now);

        await _db.SaveChangesAsync(cancellationToken);

        _accountMailer.Send(verificationEmail);

        IReadOnlyCollection<string> roleNames = [SystemRoles.Admin];
        var permissionCodes = SystemRolePermissions.Map[SystemRoles.Admin].Order(StringComparer.Ordinal).ToArray();

        return CreateAuthResponse(user, roleNames, permissionCodes, refreshToken, refreshTokenEntity);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);

        // Login anında henüz bir tenant yok: kullanıcının hangi şirkete ait olduğunu
        // tam da bu sorguyla öğreniyoruz. Faz 3'te eklenecek tenant filtresini
        // bu yüzden bilerek devre dışı bırakıyoruz.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        // Kullanıcı yoksa bile BCrypt'i çalıştırıyoruz ki iki durum aynı sürede bitsin.
        var hasPassword = !string.IsNullOrEmpty(user?.PasswordHash);
        var passwordHash = hasPassword ? user!.PasswordHash : GetDummyPasswordHash();
        var passwordIsValid = _passwordHasher.Verify(request.Password, passwordHash);

        if (user is null || !hasPassword || !passwordIsValid)
        {
            throw new UnauthorizedException(ErrorCodes.Auth.InvalidCredentials, InvalidCredentialsMessage);
        }

        // Bu noktaya sadece şifreyi bilen biri gelebilir; artık hesabın durumunu söylemek güvenli.
        if (!IsAllowedToSignIn(user))
        {
            throw new ForbiddenException(ErrorCodes.Auth.AccountDisabled, "This account has been disabled.");
        }

        var now = _timeProvider.GetUtcNow();

        user.LastLoginAt = now;

        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(user, ipAddress, now);
        _db.RefreshTokens.Add(refreshTokenEntity);

        await _db.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, GetRoleNames(user), GetPermissionCodes(user), refreshToken, refreshTokenEntity);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await _refreshTokenValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tokenHash = _secureTokenService.HashToken(request.RefreshToken);
        var now = _timeProvider.GetUtcNow();

        // Access token'ın süresi dolmuş olabilir, yani bu istekte de tenant bilgisi yok.
        // Tenant'ı refresh token kaydının kendisinden öğreniyoruz.
        var existingToken = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedException(ErrorCodes.Auth.InvalidRefreshToken, InvalidRefreshTokenMessage);
        }

        // Reuse detection: bu token daha önce kullanılıp yenisiyle değiştirilmiş.
        // Tekrar gelmesi, kopyasının başka birinde olduğunu gösterir.
        if (existingToken.ReplacedByTokenId is not null)
        {
            var revokedCount = await RevokeAllActiveTokensAsync(
                existingToken.UserId, ipAddress, now, cancellationToken);

            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId} from {IpAddress}. Revoked {RevokedCount} active session(s).",
                existingToken.UserId, ipAddress, revokedCount);

            throw new UnauthorizedException(ErrorCodes.Auth.InvalidRefreshToken, InvalidRefreshTokenMessage);
        }

        if (existingToken.RevokedAt is not null || existingToken.ExpiresAt <= now)
        {
            throw new UnauthorizedException(ErrorCodes.Auth.InvalidRefreshToken, InvalidRefreshTokenMessage);
        }

        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(u => u.Id == existingToken.UserId && !u.IsDeleted, cancellationToken);

        if (user is null || !IsAllowedToSignIn(user))
        {
            throw new UnauthorizedException(ErrorCodes.Auth.InvalidRefreshToken, InvalidRefreshTokenMessage);
        }

        var (newRefreshToken, newRefreshTokenEntity) = CreateRefreshToken(user, ipAddress, now);

        // Atomik "sahiplenme": kontrol (RevokedAt IS NULL) ve güncelleme tek SQL cümlesinde.
        // Aynı token ile aynı anda gelen iki istekten yalnızca biri 1 satır günceller.
        var claimedRows = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.Id == existingToken.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, (DateTimeOffset?)now)
                    .SetProperty(t => t.RevokedByIp, ipAddress)
                    .SetProperty(t => t.ReplacedByTokenId, (Guid?)newRefreshTokenEntity.Id)
                    .SetProperty(t => t.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);

        if (claimedRows == 0)
        {
            throw new UnauthorizedException(ErrorCodes.Auth.InvalidRefreshToken, InvalidRefreshTokenMessage);
        }

        _db.RefreshTokens.Add(newRefreshTokenEntity);

        await _db.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, GetRoleNames(user), GetPermissionCodes(user), newRefreshToken, newRefreshTokenEntity);
    }

    public async Task LogoutAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await _refreshTokenValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tokenHash = _secureTokenService.HashToken(request.RefreshToken);
        var now = _timeProvider.GetUtcNow();

        // Token bulunamasa ya da zaten iptal edilmiş olsa da hata vermiyoruz:
        // logout'un sonucu her durumda aynıdır, "bu token artık çalışmıyor".
        await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.TokenHash == tokenHash && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, (DateTimeOffset?)now)
                    .SetProperty(t => t.RevokedByIp, ipAddress)
                    .SetProperty(t => t.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);
    }

        public async Task<AuthUserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (_tenantContext.UserId is not Guid userId)
        {
            throw new UnauthorizedException("Authentication is required.");
        }
        
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive && u.Company.IsActive)
            .Select(u => new AuthUserDto(
                u.Id,
                u.CompanyId,
                u.Company.Name,
                u.Email,
                u.IsEmailVerified,
                u.FirstName,
                u.LastName,
                u.UserRoles
                    .Select(ur => ur.Role.Name)
                    .OrderBy(name => name)
                    .ToList(),
                u.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Code)
                    .Distinct()
                    .OrderBy(code => code)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return user ?? throw new UnauthorizedException(
            "The user associated with this token is no longer available.");
    }

    public async Task<InvitationPreviewDto> PreviewInvitationAsync(
        InvitationTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _invitationTokenValidator.ValidateAndThrowAsync(request, cancellationToken);

        var token = await _userTokens.FindUsableAsync(
            request.Token, UserTokenType.Invitation, _timeProvider.GetUtcNow(), cancellationToken);

        var user = token is null ? null : await FindInvitedUserAsync(token.UserId, cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleException(ErrorCodes.Auth.InvalidToken, InvalidInvitationMessage);
        }

        return new InvitationPreviewDto(user.Email, user.FirstName, user.LastName, user.Company.Name);
    }

    public async Task<AuthResponse> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await _acceptInvitationValidator.ValidateAndThrowAsync(request, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var token = await _userTokens.ConsumeAsync(
            request.Token, UserTokenType.Invitation, now, cancellationToken);

        var user = token is null ? null : await FindInvitedUserAsync(token.UserId, cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleException(ErrorCodes.Auth.InvalidToken, InvalidInvitationMessage);
        }

        user.PasswordHash = _passwordHasher.Hash(request.Password);
        user.IsEmailVerified = true;
        user.LastLoginAt = now;

        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(user, ipAddress, now);
        _db.RefreshTokens.Add(refreshTokenEntity);

        await _db.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, GetRoleNames(user), GetPermissionCodes(user), refreshToken, refreshTokenEntity);
    }

    private async Task<User?> FindInvitedUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        return user is not null && IsAllowedToSignIn(user) && string.IsNullOrEmpty(user.PasswordHash)
            ? user
            : null;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsAllowedToSignIn(User user) =>
        user.IsActive && user.Company.IsActive && !user.Company.IsDeleted;

    private static IReadOnlyCollection<string> GetRoleNames(User user) => user.UserRoles
        .Select(ur => ur.Role.Name)
        .OrderBy(name => name)
        .ToArray();

    private static IReadOnlyCollection<string> GetPermissionCodes(User user) => user.UserRoles
        .SelectMany(ur => ur.Role.RolePermissions)
        .Select(rp => rp.Permission.Code)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    private string GetDummyPasswordHash() =>
        _dummyPasswordHash ??= _passwordHasher.Hash(_secureTokenService.GenerateToken());

    private Task<int> RevokeAllActiveTokensAsync(
        Guid userId,
        string? ipAddress,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, (DateTimeOffset?)now)
                    .SetProperty(t => t.RevokedByIp, ipAddress)
                    .SetProperty(t => t.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);
    }

    private async Task<string> GenerateUniqueSlugAsync(string companyName, CancellationToken cancellationToken)
    {
        var slug = SlugGenerator.Generate(companyName);

        var exists = await _db.Companies
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Slug == slug, cancellationToken);

        return exists ? $"{slug}-{Guid.NewGuid().ToString("N")[..6]}" : slug;
    }

    private static List<Role> CreateSystemRoles(
        Guid companyId,
        IReadOnlyDictionary<string, Permission> permissionsByCode)
    {
        var roles = new List<Role>();

        foreach (var (roleName, permissionCodes) in SystemRolePermissions.Map)
        {
            var role = new Role
            {
                CompanyId = companyId,
                Name = roleName,
                IsSystemRole = true
            };

            foreach (var code in permissionCodes)
            {
                if (!permissionsByCode.TryGetValue(code, out var permission))
                {
                    throw new InvalidOperationException(
                        $"Permission '{code}' is missing from the catalog. " +
                        "Has the SeedPermissionCatalog migration been applied?");
                }

                role.RolePermissions.Add(new RolePermission
                {
                    CompanyId = companyId,
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }

            roles.Add(role);
        }

        return roles;
    }

    private (string Token, RefreshToken Entity) CreateRefreshToken(
        User user,
        string? ipAddress,
        DateTimeOffset now)
    {
        var token = _secureTokenService.GenerateToken();

        var entity = new RefreshToken
        {
            CompanyId = user.CompanyId,
            UserId = user.Id,
            TokenHash = _secureTokenService.HashToken(token),
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        return (token, entity);
    }

    private AuthResponse CreateAuthResponse(
        User user,
        IReadOnlyCollection<string> roleNames,
        IReadOnlyCollection<string> permissionCodes,
        string refreshToken,
        RefreshToken refreshTokenEntity)
    {
        var accessToken = _jwtTokenGenerator.Generate(user, roleNames);

        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken,
            refreshTokenEntity.ExpiresAt,
            ToDto(user, roleNames, permissionCodes));
    }

    private static AuthUserDto ToDto(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions) => new(
        user.Id,
        user.CompanyId,
        user.Company.Name,
        user.Email,
        user.IsEmailVerified,
        user.FirstName,
        user.LastName,
        roles,
        permissions);
}