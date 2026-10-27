using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PriceHunt.API.Mapping;
using PriceHunt.Application.Searches.Models;

namespace PriceHunt.API.Streaming;

/// <summary>
/// Writes the updates of a search to the response as a stream of server-sent events.
/// </summary>
public sealed class SearchEventStreamResult : IActionResult
{
    private readonly ShippingSearchExecution _execution;
    private readonly ApiMapper _mapper;

    public SearchEventStreamResult(ShippingSearchExecution execution, ApiMapper mapper)
    {
        _execution = execution;
        _mapper = mapper;
    }

    /// <summary>
    /// Writes each update as an event, and waits for the search to be stored before the response ends.
    /// </summary>
    /// <param name="context">The current request and response.</param>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var http = context.HttpContext;
        var jsonOptions = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        async Task SendAsync(string eventName, object data)
        {
            var json = JsonSerializer.Serialize(data, jsonOptions);
            await http.Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", http.RequestAborted);
            await http.Response.Body.FlushAsync(http.RequestAborted);
        }

        try
        {
            await foreach (var update in _execution.Events.WithCancellation(http.RequestAborted))
            {
                switch (update)
                {
                    case ShippingSearchUpdate.Started started:
                        await SendAsync("started", _mapper.ToEvent(started));
                        break;
                    case ShippingSearchUpdate.SupplierResponded responded:
                        await SendAsync("supplier", _mapper.ToEvent(responded));
                        break;
                    case ShippingSearchUpdate.Finished finished:
                        await SendAsync("finished", _mapper.ToEvent(finished));
                        break;
                }
            }
        }
        finally
        {
            await _execution.Completed;
        }
    }
}
