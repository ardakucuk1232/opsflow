using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Models;
using OpsFlow.Application.Common.Options;
using OpsFlow.Application.Common.Security;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Identity;

public sealed class JwtTokenGenerator: IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;

        var key = new SymmetricSecurityKey(Convert.FromBase64String(_options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessToken Generate(User user, IReadOnlyCollection<string> roles)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            [OpsFlowClaimTypes.UserId] = user.Id.ToString(),
            [OpsFlowClaimTypes.CompanyId] = user.CompanyId.ToString(),
            [OpsFlowClaimTypes.Email] = user.Email,
            [JwtRegisteredClaimNames.GivenName] = user.FirstName,
            [JwtRegisteredClaimNames.FamilyName] = user.LastName,

            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),

            [OpsFlowClaimTypes.Role] = roles.ToArray()
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = _signingCredentials
        };

        var token = _tokenHandler.CreateToken(descriptor);

        return new AccessToken(token, expiresAt);
    }
}