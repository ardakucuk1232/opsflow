using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpsFlow.Application.Common.Options;
using OpsFlow.Application.Common.Security;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Identity;

namespace OpsFlow.UnitTests.Identity;

public sealed class JwtTokenGeneratorTests
{
    private static readonly JwtOptions TestOptions = new()
    {
        Issuer = "OpsFlow.Tests",
        Audience = "OpsFlow.Tests.Client",
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        AccessTokenMinutes = 15
    };

    [Fact]
    public void Generate_ShouldEmbedUserAndTenantClaims()
    {
        var user = CreateUser();
        var generator = CreateGenerator(TimeProvider.System);

        var result = generator.Generate(user, ["Admin"]);

        var jwt = new JsonWebToken(result.Token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.CompanyId.ToString(), jwt.GetClaim(OpsFlowClaimTypes.CompanyId).Value);
        Assert.Equal(user.Email, jwt.GetClaim(OpsFlowClaimTypes.Email).Value);
        Assert.Contains(jwt.Claims, c => c.Type == OpsFlowClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void Generate_ShouldExpireAfterConfiguredMinutes()
    {
        var now = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var generator = CreateGenerator(new FixedTimeProvider(now));

        var result = generator.Generate(CreateUser(), []);

        var jwt = new JsonWebToken(result.Token);
        var expected = now.AddMinutes(15);

        Assert.Equal(expected, result.ExpiresAt);
        Assert.Equal(expected.UtcDateTime, jwt.ValidTo);
    }

    [Fact]
    public async Task Generate_TokenShouldPassValidationWithTheSameKey()
    {
        var generator = CreateGenerator(TimeProvider.System);
        var result = generator.Generate(CreateUser(), ["Employee"]);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(
            result.Token,
            CreateValidationParameters(Convert.FromBase64String(TestOptions.SigningKey)));

        Assert.True(validation.IsValid, validation.Exception?.Message);
    }

    [Fact]
    public async Task Generate_TokenShouldFailValidationWithADifferentKey()
    {
        var generator = CreateGenerator(TimeProvider.System);
        var result = generator.Generate(CreateUser(), ["Admin"]);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(
            result.Token,
            CreateValidationParameters(RandomNumberGenerator.GetBytes(32)));

        Assert.False(validation.IsValid);
    }

    private static JwtTokenGenerator CreateGenerator(TimeProvider timeProvider) =>
        new(Options.Create(TestOptions), timeProvider);

    private static User CreateUser() => new()
    {
        CompanyId = Guid.CreateVersion7(),
        Email = "ali@abc.com",
        FirstName = "Ali",
        LastName = "Yilmaz"
    };

    private static TokenValidationParameters CreateValidationParameters(byte[] key) => new()
    {
        ValidIssuer = TestOptions.Issuer,
        ValidAudience = TestOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}