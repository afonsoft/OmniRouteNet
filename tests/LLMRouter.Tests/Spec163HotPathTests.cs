using LLMRouter.Core.Data;
using LLMRouter.Core.Gateway;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LLMRouter.Tests;

/// <summary>SPEC-163: WAL pragmas, cached hot reads, budget spend cache,
/// incremental free-plan counter.</summary>
public class Spec163HotPathTests : WebAppTestBase
{
    [Fact]
    public async Task SqliteConnectionUsesWalAndBusyTimeout()
    {
        await using var db = Db();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode";
        (await cmd.ExecuteScalarAsync())!.ToString().ShouldBe("wal");
        cmd.CommandText = "PRAGMA busy_timeout";
        Convert.ToInt32(await cmd.ExecuteScalarAsync()).ShouldBe(5000);
        cmd.CommandText = "PRAGMA synchronous";
        Convert.ToInt32(await cmd.ExecuteScalarAsync()).ShouldBe(1); // NORMAL
    }

    [Fact]
    public async Task AliasResolvesViaCache()
    {
        await using (var db = Db())
        {
            db.Kv.Add(new KvEntry
            {
                Scope = HotReads.AliasScope, Key = "fast",
                Value = """{"target":"openai/gpt-4o"}""",
            });
            await db.SaveChangesAsync();
        }
        await using var db2 = Db();
        (await HotReads.AliasAsync(db2, "fast")).ShouldBe("openai/gpt-4o");
        (await HotReads.AliasAsync(db2, "other")).ShouldBe("other");
        (await HotReads.AliasAsync(db2, "fast")).ShouldBe("openai/gpt-4o");
    }

    [Fact]
    public async Task BudgetSpendVerdictIsCached()
    {
        await using (var db = Db())
        {
            db.UsageHistory.Add(new UsageRecord
            {
                Timestamp = DateTime.UtcNow.ToString("O"), ApiKey = "k1",
                Model = "m", Cost = 5.0, Status = "200", PromptTokens = 10,
                CompletionTokens = 5,
            });
            await db.SaveChangesAsync();
        }
        var b = new Budget
        {
            Id = "b1", Name = "b", Scope = "key", OwnerId = "k1",
            LimitUsd = 10, Period = "monthly", Mode = "hard", Enabled = true,
        };
        await using var db2 = Db();
        var since = LLMRouter.Core.Budgets.BudgetGate.PeriodStart("monthly", DateTime.UtcNow);
        var misses = HotCache.Default.Misses;
        var (usd1, tok1) = await HotReads.BudgetSpendAsync(db2, b, since);
        var (usd2, _) = await HotReads.BudgetSpendAsync(db2, b, since);
        usd1.ShouldBe(5.0); usd2.ShouldBe(5.0); tok1.ShouldBe(15);
        // second call must be a cache hit — no second SUM query
        HotCache.Default.Misses.ShouldBe(misses + 1);
    }

    [Fact]
    public async Task FreeDailyCounterIncrements()
    {
        var uid = $"u-{Guid.NewGuid():N}";
        await using (var db = Db())
        {
            await HotReads.CreditFreeDailyAsync(db, uid);
            await HotReads.CreditFreeDailyAsync(db, uid);
            await db.SaveChangesAsync();
        }
        await using var db2 = Db();
        (await HotReads.FreeDailyUsedAsync(db2, uid)).ShouldBe(2);
    }

    [Fact]
    public async Task FreeDailySeedsFromHistoryOnFirstRead()
    {
        var uid = $"u-{Guid.NewGuid():N}";
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await using (var db = Db())
        {
            db.UsageHistory.Add(new UsageRecord
            {
                Timestamp = today + " 10:00:00", ApiKey = "k", Model = "m",
                Status = "200", UserId = uid,
            });
            await db.SaveChangesAsync();
        }
        await using var db2 = Db();
        (await HotReads.FreeDailyUsedAsync(db2, uid)).ShouldBe(1);
        // subsequent reads hit the seeded counter, not a re-count
        (await HotReads.FreeDailyUsedAsync(db2, uid)).ShouldBe(1);
    }
}
