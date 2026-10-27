using System.Diagnostics;

namespace PriceHunt.API.Middleware;

/// <summary>
/// Gives each request a correlation id and logs how it ended.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    /// <summary>
    /// The header that carries the correlation id, in the request and in the response.
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    private const int MaximumCorrelationIdLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Takes the correlation id from the request or makes one, then logs the request when it ends.
    /// </summary>
    /// <param name="context">The current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            _logger.Log(
                LevelFor(statusCode),
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:0} ms.",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                elapsed);
        }
    }

    private static LogLevel LevelFor(int statusCode) => statusCode switch
    {
        >= StatusCodes.Status500InternalServerError => LogLevel.Error,
        >= StatusCodes.Status400BadRequest => LogLevel.Warning,
        _ => LogLevel.Information
    };

    private static string ResolveCorrelationId(HttpContext context)
    {
        var supplied = context.Request.Headers[CorrelationIdHeader].ToString();

        if (!string.IsNullOrWhiteSpace(supplied)
            && supplied.Length <= MaximumCorrelationIdLength
            && supplied.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
        {
            return supplied;
        }

        return Guid.NewGuid().ToString("N");
    }
}
