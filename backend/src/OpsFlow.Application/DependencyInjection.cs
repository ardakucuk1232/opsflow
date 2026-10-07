using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Features.Auth;
using OpsFlow.Application.Features.Auth.Emails;
using OpsFlow.Application.Features.Auth.Tokens;

namespace OpsFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("en");

        services.AddSingleton<AccountEmailComposer>();
        services.AddScoped<UserTokenManager>();
        services.AddScoped<AccountMailer>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();

        return services;
    }
}