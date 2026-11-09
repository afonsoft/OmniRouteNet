using LLMRouter.Core.Data;
using LLMRouter.Core.Routing;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LLMRouter.Tests;

/// <summary>SPEC-165 RF-013a (upstream #19123): a complete usage reading that
/// no longer reports a previously persisted quota window retires it; partial
/// readings never retire anything.</summary>
public class Spec165QuotaWindowRetireTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"llmr-{Guid.NewGuid():N}.db");

    private LlmRouterDbContext Db()
    {
        var ctx = new LlmRouterDbContext(
            new DbContextOptionsBuilder<LlmRouterDbContext>().UseSqlite($"Data Source={_dbPath}").Options);
        ctx.EnsureCreated();
        return ctx;
    }

    public void Dispose()
    {
        foreach (var f in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
            try { File.Delete(f); } catch { }
    }

    private static Dictionary<string, object?> Quota(double pct, string? reset = null) => new()
    {
        ["remainingPercentage"] = pct, ["resetAt"] = reset,
    };

    // # Covers RF-013a (AC-13: complete reading without the window retires it)
    [Fact]
    public async Task Complete_reading_without_7d_window_retires_it()
    {
        using var db = Db();
        // persisted: 7d window exhausted + 5h window ok
        await QuotaSnapshots.SaveAsync(db, "anthropic", "c1", new Dictionary<string, QuotaSnapshots.Window>
        {
            ["5h"] = new(50, false),
            ["7d"] = new(0, true),
        });

        // complete probe reading now carries ONLY 5h
        var retired = await QuotaSnapshots.RetireUnreportedAsync(db, "anthropic", "c1", ["5h"]);

        retired.ShouldHaveSingleItem();
        retired[0].ShouldBe("7d");

        // latest ACTIVE snapshot no longer carries 7d → connection is not exhausted anymore
        var active = await QuotaSnapshots.LatestActiveAsync(db);
        active.Select(s => s.WindowKey).ShouldBe(["5h"]);
        active.All(s => s.IsExhausted == 0).ShouldBeTrue();

        // history is kept (LatestAsync still sees the retirement record)
        var latest = await QuotaSnapshots.LatestAsync(db);
        latest.Count(s => s.WindowKey == "7d").ShouldBe(1);
        QuotaSnapshots.IsRetired(latest.Single(s => s.WindowKey == "7d")).ShouldBeTrue();
    }

    // # Covers RF-013a (partial evidence never retires)
    [Fact]
    public async Task RetireUnreportedAsync_with_no_reported_keys_retires_nothing()
    {
        using var db = Db();
        await QuotaSnapshots.SaveAsync(db, "anthropic", "c1", new Dictionary<string, QuotaSnapshots.Window>
        {
            ["5h"] = new(50, false), ["7d"] = new(0, true),
        });

        // header capture = partial evidence: empty reported set must NOT prove absence
        var retired = await QuotaSnapshots.RetireUnreportedAsync(db, "anthropic", "c1", Array.Empty<string>());
        retired.ShouldBeEmpty();
        (await QuotaSnapshots.LatestActiveAsync(db)).Select(s => s.WindowKey)
            .ShouldContain("7d");
    }

    // # Covers RF-013a (still-reported windows are kept)
    [Fact]
    public async Task Window_still_reported_is_not_retired()
    {
        using var db = Db();
        await QuotaSnapshots.SaveAsync(db, "openai", "c2", new Dictionary<string, QuotaSnapshots.Window>
        {
            ["5h"] = new(80, false), ["7d"] = new(10, false),
        });

        var retired = await QuotaSnapshots.RetireUnreportedAsync(db, "openai", "c2", ["5h", "7d"]);
        retired.ShouldBeEmpty();
        (await QuotaSnapshots.LatestActiveAsync(db)).Count.ShouldBe(2);
    }

    // # Covers RF-013a (retirement is idempotent: restart does not rehydrate)
    [Fact]
    public async Task Retire_is_idempotent_across_restarts()
    {
        using var db = Db();
        await QuotaSnapshots.SaveAsync(db, "anthropic", "c1", new Dictionary<string, QuotaSnapshots.Window>
        {
            ["7d"] = new(0, true),
        });
        await QuotaSnapshots.RetireUnreportedAsync(db, "anthropic", "c1", Array.Empty<string>());

        // second run: no duplicate retirement rows
        var again = await QuotaSnapshots.RetireUnreportedAsync(db, "anthropic", "c1", Array.Empty<string>());
        again.ShouldBeEmpty();
        (await db.QuotaSnapshots.CountAsync(s => s.Provider == "anthropic" && s.WindowKey == "7d"
            && s.RawData == QuotaSnapshots.RetiredWindowMarker)).ShouldBe(1);
    }

    // # Covers RF-013a (IsCompleteUsageReading contract)
    [Fact]
    public void IsCompleteUsageReading_matches_upstream_contract()
    {
        var complete = new Dictionary<string, object?>
        {
            ["quotasComplete"] = true,
            ["quotas"] = new Dictionary<string, object?> { ["5h"] = Quota(50) },
        };
        Server.Endpoints.QuotaCapture.IsCompleteUsageReading(complete).ShouldBeTrue();

        // error payload / stale marker / missing marker → partial
        Server.Endpoints.QuotaCapture.IsCompleteUsageReading(
            new Dictionary<string, object?> { ["message"] = "usage failed", ["quotasComplete"] = true }).ShouldBeFalse();
        Server.Endpoints.QuotaCapture.IsCompleteUsageReading(
            new Dictionary<string, object?> { ["_stale"] = true, ["quotasComplete"] = true }).ShouldBeFalse();
        Server.Endpoints.QuotaCapture.IsCompleteUsageReading(
            new Dictionary<string, object?> { ["quotas"] = new Dictionary<string, object?>() }).ShouldBeFalse();
        Server.Endpoints.QuotaCapture.IsCompleteUsageReading(null).ShouldBeFalse();
        Server.Endpoints.QuotaCapture.IsCompleteUsageReading("x").ShouldBeFalse();
    }
}
