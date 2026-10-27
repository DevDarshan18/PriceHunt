using System.Net.Http.Json;
using System.Text.Json;
using PriceHunt.Infrastructure.Locations;

namespace PriceHunt.Tests.Api.Support;

public sealed record TestRoute(string Origin, string Destination)
{
    public string Query => $"origin={Uri.EscapeDataString(Origin)}&destination={Uri.EscapeDataString(Destination)}";
}

public static class ApiTestHelpers
{
    private static int _routeCount;

    public static TestRoute NewRoute()
    {
        var places = KnownLocations.All;
        var index = Interlocked.Increment(ref _routeCount);
        var origin = index % places.Count;
        var destination = (origin + 1 + index / places.Count) % places.Count;
        return new TestRoute(places[origin], places[destination]);
    }

    public static object SearchBody(TestRoute route, params Guid[]? supplierIds)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new
        {
            fromLocation = route.Origin,
            toLocation = route.Destination,
            fromDate = today.AddDays(1),
            toDate = today.AddDays(5),
            supplierIds
        };
    }

    public static async Task<List<(string Name, JsonElement Data)>> SearchAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/searches", body);
        return ParseEvents(await response.Content.ReadAsStringAsync());
    }

    public static List<(string Name, JsonElement Data)> ParseEvents(string body) => body
        .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
        .Select(block => block.Split('\n'))
        .Select(lines => (
            Name: lines[0]["event: ".Length..],
            Data: JsonDocument.Parse(lines[1]["data: ".Length..]).RootElement.Clone()))
        .ToList();

    public static async Task<JsonElement> ReadUntilEventAsync(
        HttpClient client,
        object body,
        string eventName,
        CancellationToken cancellationToken,
        CancellationTokenSource? disconnectAfterEvent = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/searches") { Content = JsonContent.Create(body) };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        var matched = false;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (line == $"event: {eventName}")
            {
                matched = true;
            }
            else if (matched && line.StartsWith("data: ", StringComparison.Ordinal))
            {
                var data = JsonDocument.Parse(line["data: ".Length..]).RootElement.Clone();
                disconnectAfterEvent?.Cancel();
                return data;
            }
        }

        throw new InvalidOperationException($"The stream ended without a '{eventName}' event.");
    }
}
