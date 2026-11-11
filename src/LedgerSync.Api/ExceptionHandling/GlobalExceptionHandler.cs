using FluentValidation;
using LedgerSync.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSync.Api.ExceptionHandling;

/// <summary>
/// Converts exceptions into RFC 7807 problem details.
/// Known business errors get precise status codes; anything else is a 500 without internal details.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Client went away: nothing to answer.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {Path} was cancelled by the client", httpContext.Request.Path);
            return true;
        }

        var problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),
            OrderNotFoundException or SyncRunNotFoundException =>
                Problem(StatusCodes.Status404NotFound, "Not found", exception.Message),
            SyncAlreadyRunningException =>
                Problem(StatusCodes.Status409Conflict, "Synchronization is already running", exception.Message),
            PersistenceConflictException =>
                Problem(StatusCodes.Status409Conflict, "Data conflict", exception.Message),
            BadHttpRequestException badRequest =>
                Problem(badRequest.StatusCode, "Bad request", exception.Message),
            _ => Problem(StatusCodes.Status500InternalServerError, "Internal server error",
                "An unexpected error occurred. Use traceId to find details in the logs.")
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Request {Method} {Path} failed with {StatusCode}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, problem.Status, exception.Message);

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed"
        };
    }
}
