namespace PriceHunt.API.Middleware;

/// <summary>
/// Adds the API's middleware to the request pipeline.
/// </summary>
public static class MiddlewareApplicationBuilderExtensions
{
    /// <summary>
    /// Adds request logging and error handling. Logging goes first so it also sees the failures that error handling turns into responses.
    /// </summary>
    /// <param name="app">The pipeline to add the middleware to.</param>
    /// <returns>The same pipeline, so calls can be chained.</returns>
    public static IApplicationBuilder UsePriceHuntMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }
}
