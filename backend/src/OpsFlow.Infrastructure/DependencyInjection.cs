using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Options;
using OpsFlow.Infrastructure.Identity;
using OpsFlow.Infrastructure.Persistence;
using OpsFlow.Infrastructure.Persistence.Interceptors;
using OpsFlow.Infrastructure.Tenancy;

namespace OpsFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Birden fazla bölüm kullandığı için en üstte.
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        AddPersistence(services, configuration);
        AddIdentityServices(services, configuration);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured. " +
                "Set it with: dotnet user-secrets set \"ConnectionStrings:Postgres\" \"<value>\" " +
                "--project src/OpsFlow.Api");
        }

        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddScoped<TenantGuardInterceptor>();

        services.AddDbContext<OpsFlowDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(OpsFlowDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            });

            options.AddInterceptors(
                serviceProvider.GetRequiredService<TenantGuardInterceptor>(),
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IOpsFlowDbContext>(sp => sp.GetRequiredService<OpsFlowDbContext>());
    }

    private static void AddIdentityServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
            .Validate(o => JwtOptions.HasValidSigningKey(o.SigningKey),
                "Jwt:SigningKey must be a Base64 string of at least 32 bytes. " +
                "Set it with: dotnet user-secrets set \"Jwt:SigningKey\" \"<value>\" --project src/OpsFlow.Api")
            .Validate(o => o.AccessTokenMinutes > 0, "Jwt:AccessTokenMinutes must be positive.")
            .Validate(o => o.RefreshTokenDays > 0, "Jwt:RefreshTokenDays must be positive.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
    }
}