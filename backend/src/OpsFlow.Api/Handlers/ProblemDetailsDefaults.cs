using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Api.Handlers;

public static class ProblemDetailsDefaults
{
    public const string CodeKey = "code";

    public static void Configure(ProblemDetailsOptions options)
    {
        options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;

            if (problem.Extensions.ContainsKey(CodeKey))
            {
                return;
            }

            var code = CodeFor(problem.Status ?? context.HttpContext.Response.StatusCode);

            if (code is not null)
            {
                problem.Extensions[CodeKey] = code;
            }
        };
    }

    private static string? CodeFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ErrorCodes.BadRequest,
        StatusCodes.Status401Unauthorized => ErrorCodes.Unauthorized,
        StatusCodes.Status403Forbidden => ErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => ErrorCodes.NotFound,
        StatusCodes.Status409Conflict => ErrorCodes.Conflict,
        StatusCodes.Status422UnprocessableEntity => ErrorCodes.BusinessRuleViolation,
        StatusCodes.Status429TooManyRequests => ErrorCodes.TooManyRequests,
        >= StatusCodes.Status500InternalServerError => ErrorCodes.InternalError,
        _ => null
    };
}
