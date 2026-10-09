using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Options;
using OpsFlow.Infrastructure.Email;
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
        AddEmail(services, configuration);

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
        services.AddScoped<ITaskNumberGenerator, TaskNumberGenerator>();
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

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Host),
                "Email:Host is required. Set it in configuration or with the Email__Host environment variable.")
            .Validate(o => o.Port is > 0 and <= 65535, "Email:Port must be between 1 and 65535.")
            .Validate(o => SmtpOptions.HasValidFromAddress(o.FromAddress),
                "Email:FromAddress must be a valid email address.")
            .Validate(o => o.TimeoutSeconds > 0, "Email:TimeoutSeconds must be positive.")
            .ValidateOnStart();

        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection(FrontendOptions.SectionName))
            .Validate(o => FrontendOptions.HasValidBaseUrl(o.BaseUrl),
                "Frontend:BaseUrl must be an absolute http or https URL.")
            .ValidateOnStart();

        services.AddOptions<AccountOptions>()
            .Bind(configuration.GetSection(AccountOptions.SectionName))
            .Validate(o => o.EmailVerificationTokenHours > 0, "Account:EmailVerificationTokenHours must be positive.")
            .Validate(o => o.PasswordResetTokenMinutes > 0, "Account:PasswordResetTokenMinutes must be positive.")
            .Validate(o => o.InvitationTokenDays > 0, "Account:InvitationTokenDays must be positive.")
            .Validate(o => o.EmailCooldownSeconds >= 0, "Account:EmailCooldownSeconds cannot be negative.")
            .ValidateOnStart();

        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddHostedService<EmailDispatchService>();
    }
}
