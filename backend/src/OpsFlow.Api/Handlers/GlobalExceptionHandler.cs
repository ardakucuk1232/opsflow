using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Api.Handlers;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),

            NotFoundException e => CreateProblem(StatusCodes.Status404NotFound, "Resource not found.", e),
            ConflictException e => CreateProblem(StatusCodes.Status409Conflict, "Conflict.", e),
            UnauthorizedException e => CreateProblem(StatusCodes.Status401Unauthorized, "Unauthorized.", e),
            ForbiddenException e => CreateProblem(StatusCodes.Status403Forbidden, "Forbidden.", e),
            BusinessRuleException e => CreateProblem(StatusCodes.Status422UnprocessableEntity, "Business rule violation.", e),

            _ => CreateProblem(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                detail: null,
                ErrorCodes.InternalError)
        };

        if (problem.Status >= 500)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Request failed with {StatusCode}: {Message}",
                problem.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails CreateProblem(int status, string title, DomainException exception) =>
        CreateProblem(status, title, exception.Message, exception.Code);

    private static ProblemDetails CreateProblem(int status, string title, string? detail, string code) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { [ProblemDetailsDefaults.CodeKey] = code }
    };

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Extensions = { [ProblemDetailsDefaults.CodeKey] = ErrorCodes.ValidationFailed }
        };
    }
}