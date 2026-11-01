using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LLMRouter.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LLMRouter.Tests;

/// <summary>SPEC-164: SSE pass-through w/o per-chunk usage parse, coalesced
/// flush, response compression for non-SSE bodies.</summary>
public class Spec164StreamTests : WebAppTestBase
{
    private sealed class SseUpstream : HttpMessageHandler
    {
        // two deltas w/o "usage" (skipped by TrackUsage), one usage chunk, [DONE]
        private const string Payload =
            "data: {\"id\":\"c1\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"delta\":{\"content\":\"he\"},\"index\":0}]}\n\n"
            + "data: {\"id\":\"c1\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"delta\":{\"content\":\"llo\"},\"index\":0}]}\n\n"
            + "data: {\"id\":\"c1\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"delta\":{},\"index\":0}],\"usage\":{\"prompt_tokens\":5,\"completion_tokens\":7}}\n\n"
            + "data: [DONE]\n\n";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(Payload, Encoding.UTF8, "text/event-stream") });
    }

    protected override void ConfigureTestServices(IServiceCollection s)
        => s.AddHttpClient("upstream").ConfigurePrimaryHttpMessageHandler(() => new SseUpstream());

    private async Task SeedAsync()
    {
        await using var db = Db();
        db.EnsureCreated();
        db.ProviderConnections.Add(new ProviderConnection
        {
            Id = "c1", Provider = "openai", AuthType = "apikey", Name = "main",
            IsActive = true,
            Data = """{"baseUrl":"http://fake","apiKey":"sk-fake"}""",
            CreatedAt = "x", UpdatedAt = "x",
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Sse_stream_passes_chunks_through_and_tracks_usage()
    {
        await SeedAsync();
        await LoginAsync();
        var r = await Client.PostAsJsonAsync("/v1/providers/openai/chat/completions",
            new { model = "gpt-4o", stream = true, messages = new[] { new { role = "user", content = "hi" } } });
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await r.Content.ReadAsStringAsync();
        body.ShouldContain("he");
        body.ShouldContain("llo");
        body.ShouldContain("\"prompt_tokens\":5");
        body.ShouldContain("data: [DONE]");
        // usage writer is off-path — poll briefly for the row
        UsageRecord? u = null;
        for (var i = 0; i < 40 && u is null; i++)
        {
            await Task.Delay(250);
            await using var db = Db();
            u = await db.UsageHistory.FirstOrDefaultAsync();
        }
        u.ShouldNotBeNull();
        u.PromptTokens.ShouldBe(5);
        u.CompletionTokens.ShouldBe(7);
    }

    [Fact]
    public async Task Json_response_is_gzip_compressed_when_accepted()
    {
        await SeedAsync();
        await LoginAsync();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/health/ping");
        req.Headers.Add("Accept-Encoding", "gzip");
        var r = await Client.SendAsync(req);
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        r.Content.Headers.ContentEncoding.ShouldContain("gzip");
    }

    [Fact]
    public async Task Sse_response_is_not_compressed()
    {
        await SeedAsync();
        await LoginAsync();
        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/providers/openai/chat/completions")
        {
            Content = JsonContent.Create(new
            { model = "gpt-4o", stream = true, messages = new[] { new { role = "user", content = "hi" } } }),
        };
        req.Headers.Add("Accept-Encoding", "gzip");
        var r = await Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        r.Content.Headers.ContentEncoding.ShouldBeEmpty();
    }
}
