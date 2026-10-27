using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace PriceHunt.API.Middleware;

/// <summary>
/// Turns whatever a request throws into the matching problem response, and logs it.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    /// <summary>
    /// Status code used when the client closed the connection before the answer was ready.
    /// </summary>
    public const int ClientClosedRequest = 499;

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Runs the rest of the pipeline and handles what it throws.
    /// </summary>
    /// <param name="context">The current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request was cancelled by the client.");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ClientClosedRequest;
            }
        }
        catch (ValidationException exception)
        {
            _logger.LogWarning("Request rejected with {ErrorCount} validation errors.", exception.Errors.Count());

            var errors = exception.Errors
                .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());
            await WriteProblemAsync(context, new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = "See the errors property for details."
            });
        }
        catch (BadHttpRequestException exception)
        {
            _logger.LogWarning("Request rejected: {Reason}", exception.Message);

            await WriteProblemAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request is not valid.",
                Detail = "The request could not be read."
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing the request.");

            await WriteProblemAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The server could not complete the request. Quote the trace id when reporting this."
            });
        }
    }

    private async Task WriteProblemAsync(HttpContext context, ProblemDetails problem)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response had already started, so the connection is being aborted.");
            context.Abort();
            return;
        }

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }
}
