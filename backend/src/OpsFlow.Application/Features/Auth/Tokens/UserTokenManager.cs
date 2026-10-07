using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Auth.Tokens;

public sealed class UserTokenManager
{
    private readonly IOpsFlowDbContext _db;
    private readonly ISecureTokenService _secureTokenService;

    public UserTokenManager(IOpsFlowDbContext db, ISecureTokenService secureTokenService)
    {
        _db = db;
        _secureTokenService = secureTokenService;
    }

    public string Issue(User user, UserTokenType type, DateTimeOffset now, TimeSpan lifetime)
    {
        var token = _secureTokenService.GenerateToken();

        _db.UserTokens.Add(new UserToken
        {
            CompanyId = user.CompanyId,
            UserId = user.Id,
            Type = type,
            TokenHash = _secureTokenService.HashToken(token),
            ExpiresAt = now.Add(lifetime)
        });

        return token;
    }

    public Task<bool> WasIssuedSinceAsync(
        Guid userId,
        UserTokenType type,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        return _db.UserTokens
            .IgnoreQueryFilters()
            .AnyAsync(t => t.UserId == userId && t.Type == type && t.CreatedAt > since, cancellationToken);
    }

    public Task<int> InvalidateAsync(
        Guid userId,
        UserTokenType type,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return _db.UserTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.Type == type && t.UsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.UsedAt, (DateTimeOffset?)now)
                    .SetProperty(t => t.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);
    }

    public async Task<UserToken?> ConsumeAsync(
        string token,
        UserTokenType type,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tokenHash = _secureTokenService.HashToken(token);

        var existing = await _db.UserTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash && t.Type == type, cancellationToken);

        if (existing is null || existing.UsedAt is not null || existing.ExpiresAt <= now)
        {
            return null;
        }

        var claimedRows = await _db.UserTokens
            .IgnoreQueryFilters()
            .Where(t => t.Id == existing.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.UsedAt, (DateTimeOffset?)now)
                    .SetProperty(t => t.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);

        return claimedRows == 1 ? existing : null;
    }
}
