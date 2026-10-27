using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PriceHunt.Application.Common;
using PriceHunt.Application.History.Interfaces;
using PriceHunt.Application.History.Models;
using PriceHunt.Application.Searches.Interfaces;
using PriceHunt.Domain.Searches;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Infrastructure.Simulation;
using PriceHunt.Tests.Api.Support;
using static PriceHunt.Tests.Api.Support.ApiTestHelpers;

namespace PriceHunt.Tests.Api;

public class ApiFailureTests
{
    [Fact]
    public async Task An_unexpected_error_returns_a_500_problem_with_a_trace_id_and_hides_the_details()
    {
        var logs = new CapturingLoggerProvider();
        await using var factory = new FixedPriceApiFactory
        {
            LoggerProvider = logs,
            OverrideServices = services =>
            {
                services.RemoveAll<ISearchHistoryReader>();
                services.AddScoped<ISearchHistoryReader, ThrowingHistoryReader>();
            }
        };
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/history");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(response.Headers.GetValues("X-Correlation-Id").Single(), problem.GetProperty("traceId").GetString());
        Assert.DoesNotContain("database exploded", problem.ToString());

        var error = Assert.Single(logs.Entries, entry => entry.Level == LogLevel.Error && entry.Exception is not null);
        Assert.Contains("database exploded", error.Exception!.Message);
    }

    [Fact]
    public async Task A_failure_after_the_stream_has_started_ends_the_stream_without_a_finished_event()
    {
        var logs = new CapturingLoggerProvider();
        var supplierEventRead = new TaskCompletionSource();
        await using var factory = new FixedPriceApiFactory
        {
            LoggerProvider = logs,
            OverrideServices = services =>
            {
                services.RemoveAll<IShippingSearchStore>();
                services.AddScoped<IShippingSearchStore>(_ => new FailingFinishStore(supplierEventRead.Task));
            }
        };
        using var client = factory.CreateClient();

        var received = await ReadWholeStreamAsync(
            client,
            SearchBody(NewRoute(), SimulatedSuppliers.AtlasFreightId),
            line =>
            {
                if (line.StartsWith("event: supplier"))
                {
                    supplierEventRead.TrySetResult();
                }
            });

        Assert.Contains("event: started", received);
        Assert.Contains("event: supplier", received);
        Assert.DoesNotContain("event: finished", received);
        Assert.Contains(logs.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("already started"));
    }

    private static async Task<string> ReadWholeStreamAsync(HttpClient client, object body, Action<string>? onLine = null)
    {
        var received = new StringBuilder();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/searches") { Content = JsonContent.Create(body) };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());

            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                received.AppendLine(line);
                onLine?.Invoke(line);
            }
        }
        catch (Exception exception) when (exception is IOException or HttpRequestException or InvalidOperationException)
        {
            // The server drops the connection when a stream fails after it has started.
        }

        return received.ToString();
    }

    private sealed class ThrowingHistoryReader : ISearchHistoryReader
    {
        public Task<PagedResult<HistoryItem>> QueryAsync(HistoryQuery query, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("database exploded");
    }

    private sealed class FailingFinishStore : IShippingSearchStore
    {
        private readonly Task _clientHasReadTheStream;

        public FailingFinishStore(Task clientHasReadTheStream)
        {
            _clientHasReadTheStream = clientHasReadTheStream;
        }

        public Task StartAsync(ShippingSearch search, IReadOnlyList<Supplier> suppliers, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveOutcomeAsync(Guid searchId, Supplier supplier, SupplierOutcome outcome, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public async Task FinishAsync(Guid searchId, SearchStatus status, CancellationToken cancellationToken)
        {
            await _clientHasReadTheStream.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            throw new InvalidOperationException("finish failed");
        }
    }
}
