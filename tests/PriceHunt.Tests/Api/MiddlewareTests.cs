using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PriceHunt.API.Middleware;

namespace PriceHunt.Tests.Api;

public sealed class MiddlewareTests
{
    [Fact]
    public async Task A_request_that_cannot_be_read_becomes_a_400_problem()
    {
        var context = CreateContext();
        var middleware = ExceptionMiddleware(_ => throw new BadHttpRequestException("Unexpected end of request content."));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        var body = await ReadJsonAsync(context);
        Assert.Equal("The request could not be read.", body.GetProperty("detail").GetString());
        Assert.Equal("trace-1", body.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Unexpected_errors_become_a_500_that_hides_the_exception_message()
    {
        var context = CreateContext();
        var middleware = ExceptionMiddleware(_ => throw new InvalidOperationException("secret connection detail"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var text = await ReadTextAsync(context);
        Assert.DoesNotContain("secret", text);
        Assert.Contains("trace-1", text);
    }

    [Fact]
    public async Task An_argument_exception_is_a_server_bug_not_a_client_error()
    {
        var context = CreateContext();
        var middleware = ExceptionMiddleware(_ => throw new ArgumentException("bad argument", "request"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task Client_cancellation_is_not_treated_as_an_error()
    {
        using var aborted = new CancellationTokenSource();
        var context = CreateContext();
        context.RequestAborted = aborted.Token;
        var middleware = ExceptionMiddleware(_ =>
        {
            aborted.Cancel();
            throw new OperationCanceledException(aborted.Token);
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(ExceptionHandlingMiddleware.ClientClosedRequest, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task A_cancellation_the_client_did_not_cause_is_a_500()
    {
        var context = CreateContext();
        var middleware = ExceptionMiddleware(_ => throw new OperationCanceledException());

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task Requests_without_errors_pass_through_untouched()
    {
        var context = CreateContext();
        var middleware = ExceptionMiddleware(inner =>
        {
            inner.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Fact]
    public async Task Logging_reuses_a_valid_correlation_id_and_exposes_it_on_the_trace_identifier()
    {
        var context = CreateContext();
        context.Request.Headers[RequestLoggingMiddleware.CorrelationIdHeader] = "abc-123_XYZ";
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("abc-123_XYZ", context.TraceIdentifier);
    }

    [Theory]
    [InlineData("has spaces")]
    [InlineData("inject\r\nX-Evil: 1")]
    [InlineData("")]
    public async Task Logging_replaces_an_unsafe_correlation_id_with_a_generated_one(string supplied)
    {
        var context = CreateContext();
        context.Request.Headers[RequestLoggingMiddleware.CorrelationIdHeader] = supplied;
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.NotEqual(supplied, context.TraceIdentifier);
        Assert.Equal(32, context.TraceIdentifier.Length);
    }

    [Fact]
    public async Task Logging_records_one_entry_with_the_final_status_code()
    {
        var logger = new ListLogger<RequestLoggingMiddleware>();
        var context = CreateContext();
        var middleware = new RequestLoggingMiddleware(
            inner =>
            {
                inner.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("404", entry.Message);
    }

    private static ExceptionHandlingMiddleware ExceptionMiddleware(RequestDelegate next) =>
        new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadTextAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpContext context) =>
        JsonDocument.Parse(await ReadTextAsync(context)).RootElement;

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
