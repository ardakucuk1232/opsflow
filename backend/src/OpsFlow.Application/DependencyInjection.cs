using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Features.Auth;

namespace OpsFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("en");

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}