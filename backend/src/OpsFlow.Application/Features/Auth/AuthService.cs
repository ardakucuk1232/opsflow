using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Options;
using OpsFlow.Application.Common.Text;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private static string? _dummyPasswordHash;

    private readonly IOpsFlowDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ISecureTokenService _secureTokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IOpsFlowDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ISecureTokenService secureTokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _secureTokenService = secureTokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
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
            throw new ConflictException("An account with this email address already exists.");
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
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
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

        await _db.SaveChangesAsync(cancellationToken);

        IReadOnlyCollection<string> roleNames = [SystemRoles.Admin];

        return CreateAuthResponse(user, roleNames, refreshToken, refreshTokenEntity);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);

        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        var passwordHash = user?.PasswordHash ?? GetDummyPasswordHash();
        var passwordIsValid = _passwordHasher.Verify(request.Password, passwordHash);

        if (user is null || !passwordIsValid)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!user.IsActive || !user.Company.IsActive || user.Company.IsDeleted)
        {
            throw new ForbiddenException("This account has been disabled.");
        }

        var now = _timeProvider.GetUtcNow();

        user.LastLoginAt = now;

        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(user, ipAddress, now);
        _db.RefreshTokens.Add(refreshTokenEntity);

        await _db.SaveChangesAsync(cancellationToken);

        IReadOnlyCollection<string> roleNames = user.UserRoles
            .Select(ur => ur.Role.Name)
            .OrderBy(name => name)
            .ToArray();

        return CreateAuthResponse(user, roleNames, refreshToken, refreshTokenEntity);
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId
                && u.CompanyId == companyId
                && !u.IsDeleted
                && u.IsActive
                && u.Company.IsActive
                && !u.Company.IsDeleted)
            .Select(u => new AuthUserDto(
                u.Id,
                u.CompanyId,
                u.Email,
                u.FirstName,
                u.LastName,
                u.UserRoles
                    .Select(ur => ur.Role.Name)
                    .OrderBy(name => name)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return user ?? throw new UnauthorizedException("The user associated with this token is no longer available.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private string GetDummyPasswordHash() =>
        _dummyPasswordHash ??= _passwordHasher.Hash(_secureTokenService.GenerateToken());

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
        string refreshToken,
        RefreshToken refreshTokenEntity)
    {
        var accessToken = _jwtTokenGenerator.Generate(user, roleNames);

        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken,
            refreshTokenEntity.ExpiresAt,
            ToDto(user, roleNames));
    }

    private static AuthUserDto ToDto(User user, IReadOnlyCollection<string> roles) => new(
        user.Id,
        user.CompanyId,
        user.Email,
        user.FirstName,
        user.LastName,
        roles);
}