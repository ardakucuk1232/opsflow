using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Common.Security;
using OpsFlow.Application.Features.Auth;
using OpsFlow.Application.Features.Auth.Emails;
using OpsFlow.Application.Features.Auth.Tokens;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Application.Features.Roles;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Application.Features.Users;

namespace OpsFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("en");

        services.AddScoped<ICurrentUserPermissions, CurrentUserPermissions>();
        services.AddScoped<PrivilegeGuard>();

        services.AddSingleton<AccountEmailComposer>();
        services.AddScoped<UserTokenManager>();
        services.AddScoped<AccountMailer>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ProjectAccess>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskCommentService, TaskCommentService>();

        return services;
    }
}