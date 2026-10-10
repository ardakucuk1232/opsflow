using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpsFlow.Api.Authorization;
using OpsFlow.Api.Realtime;
using OpsFlow.Application.Common.Options;
using OpsFlow.Application.Common.Security;

namespace OpsFlow.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) => 
            {
            var jwtOptions = jwtOptionsAccessor.Value;

            bearerOptions.MapInboundClaims = false;

            bearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SigningKey)),

                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),

                NameClaimType = OpsFlowClaimTypes.UserId,
                RoleClaimType = OpsFlowClaimTypes.Role
            };

            bearerOptions.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(accessToken)
                        && context.HttpContext.Request.Path.StartsWithSegments(NotificationsHub.Path))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}