using System.Text.Json;
using LLMRouter.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LLMRouter.Core.Gateway;

/// <summary>
/// SPEC-074: cached versions of the config reads the gateway hot path used to
/// run sequentially per request. All entries are invalidated by the
/// LlmRouterDbContext write hook (any non-telemetry entity change) and carry
/// short TTLs as a safety net for writes outside this process.
/// </summary>
public static class HotReads
{
    private static HotCache Cache => HotCache.Default;

    // HybridCache can't serialize an Undefined JsonElement; callers guard on
    // ValueKind == Object, so an empty object is the safe "no settings" value.
    private static readonly JsonElement EmptyObject =
        JsonDocument.Parse("{}").RootElement.Clone();

    public static Task<JsonElement> SettingsDataAsync(LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("settings:data", TimeSpan.FromSeconds(10), async () =>
        {
            var row = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
            if (row is null) return EmptyObject;
            try { return JsonDocument.Parse(row.Data).RootElement.Clone(); }
            catch { return EmptyObject; }
        });

    public static Task<ApiKey?> ApiKeyAsync(LlmRouterDbContext db, string key) =>
        Cache.GetOrAddAsync($"key:{key}", TimeSpan.FromSeconds(30), () =>
            db.ApiKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Key == key));

    /// <summary>SPEC-111: virtual key lookup (vk-…), cached like ApiKeyAsync.</summary>
    public static Task<UserKey?> UserKeyAsync(LlmRouterDbContext db, string key) =>
        Cache.GetOrAddAsync($"ukey:{key}", TimeSpan.FromSeconds(15), () =>
            db.UserKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Key == key));

    /// <summary>SPEC-111: mutações em userKeys invalidam o cache na hora
    /// (chave desativada tem que parar de autenticar imediatamente).</summary>
    public static void InvalidateUserKey(string key) => Cache.Invalidate($"ukey:{key}");

    public static Task<Combo?> ComboAsync(LlmRouterDbContext db, string name) =>
        Cache.GetOrAddAsync($"combo:{name}", TimeSpan.FromSeconds(30), () =>
            db.Combos.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name));

    public static Task<List<RateLimit>> EnabledRateLimitsAsync(LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("rl:enabled", TimeSpan.FromSeconds(15), () =>
            db.RateLimits.AsNoTracking().Where(r => r.Enabled).ToListAsync());

    public static Task<SessionPoolRow?> SessionPoolAsync(LlmRouterDbContext db, string provider) =>
        Cache.GetOrAddAsync($"sp:{provider}", TimeSpan.FromSeconds(30), () =>
            db.SessionPools.AsNoTracking().FirstOrDefaultAsync(
                p => p.Provider == provider && p.IsActive));

    public static Task<List<QuotaWindow>> QuotaWindowsAsync(LlmRouterDbContext db, string provider) =>
        Cache.GetOrAddAsync($"qw:{provider}", TimeSpan.FromSeconds(15), () =>
            db.QuotaWindows.AsNoTracking().Where(w => w.Provider == provider).ToListAsync());

    /// <summary>Cached verdict — quota checks against usageHistory are the
    /// expensive part; a 5s TTL keeps the cap near-exact.</summary>
    public static Task<bool> QuotaWindowExceededAsync(LlmRouterDbContext db, string provider) =>
        Cache.GetOrAddAsync($"qwx:{provider}", TimeSpan.FromSeconds(5), async () =>
        {
            var windows = await QuotaWindowsAsync(db, provider);
            if (windows.Count == 0) return false;
            return await Routing.QuotaWindows.ExceededAsync(db, provider, CancellationToken.None);
        }, [$"qwx:{provider}"]);

    public static Task<List<ProviderConnection>> ActiveConnsAsync(LlmRouterDbContext db, string provider) =>
        Cache.GetOrAddAsync($"conns:{provider}", TimeSpan.FromSeconds(15), () =>
            db.ProviderConnections.AsNoTracking()
                .Where(c => c.Provider == provider && c.IsActive)
                .OrderBy(c => c.Priority).ThenBy(c => c.Name).ToListAsync());

    public static Task<string?> ComboMappingAsync(LlmRouterDbContext db, string model) =>
        Cache.GetOrAddAsync($"mcm:{model}", TimeSpan.FromSeconds(30), () =>
            Routing.ComboMappings.MatchAsync(db, model));

    /// <summary>SPEC-081: disabled models per provider (kv scope disabledModels).</summary>
    public static Task<Dictionary<string, string[]>> DisabledModelsAsync(LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("disabledModels", TimeSpan.FromSeconds(30),
            () => Routing.DisabledModels.AllAsync(db));

    public static Task<List<ModelCooldown>> LiveModelCooldownsAsync(LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("mc:live", TimeSpan.FromSeconds(5), () =>
        {
            var nowIso = DateTime.UtcNow.ToString("o");
            return db.ModelCooldowns.AsNoTracking()
                .Where(mc => string.Compare(mc.Until, nowIso) > 0).ToListAsync();
        });

    public static Task<List<FallbackChain>> FallbackChainsAsync(LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("chains", TimeSpan.FromSeconds(30), () =>
            db.FallbackChains.AsNoTracking().Where(fc => fc.Active).ToListAsync());

    public static Task<Dictionary<string, HashSet<string>>> CapabilityOverridesAsync(
        LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("capover", TimeSpan.FromSeconds(30),
            () => Routing.ComboPlanner.LoadOverridesAsync(db, CancellationToken.None));

    public static Task<CompressionComboAssignment?> CompressionAssignmentAsync(
        LlmRouterDbContext db, string routingComboId) =>
        Cache.GetOrAddAsync($"ca:{routingComboId}", TimeSpan.FromSeconds(30), () =>
            db.CompressionComboAssignments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.RoutingComboId == routingComboId));

    public static Task<CompressionCombo?> CompressionComboAsync(
        LlmRouterDbContext db, string id) =>
        Cache.GetOrAddAsync($"cc:{id}", TimeSpan.FromSeconds(30), () =>
            db.CompressionCombos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id));

    /// <summary>Daily-token-cap verdict per key; 5s staleness on a soft cap.</summary>
    public static Task<bool> DailyCapExceededAsync(LlmRouterDbContext db, string apiKey, string? model = null) =>
        Cache.GetOrAddAsync($"dcap:{apiKey}:{model}", TimeSpan.FromSeconds(5),
            () => Routing.KeyQuota.DailyCapExceededAsync(db, apiKey, model),
            [$"dcap:{apiKey}"]);

    /// <summary>Tier rules (models allow-list + daily tokens) per key.</summary>
    public sealed record TierRules(List<string>? Models, int DailyTokens);

    public static async Task<TierRules> TierRulesAsync(
        LlmRouterDbContext db, JsonElement sdata, string apiKey)
    {
        var (models, dailyTokens) = await Cache.GetOrAddAsync($"tier:{apiKey}", TimeSpan.FromSeconds(30),
            () => Routing.SettingsOps.TierRulesAsync(db, sdata, apiKey));
        return new TierRules(models, dailyTokens);
    }

    /// <summary>Chaos config match result for a model (kv read cached 10s;
    /// evaluation stays per-request so a hit is deterministic).</summary>
    public static Task<string?> ChaosConfigAsync(LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("chaos:cfg", TimeSpan.FromSeconds(10), () =>
            db.Kv.AsNoTracking().Where(k => k.Scope == "chaos" && k.Key == "config")
                .Select(k => k.Value).FirstOrDefaultAsync());

    public static Task<System.Text.Json.Nodes.JsonArray> RegisteredPluginsAsync(
        LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("plugins", TimeSpan.FromSeconds(30),
            () => Extras.PluginHooks.RegisteredAsync(db));

    /// <summary>oneproxy current URL/rotation (kv read cached 10s).</summary>
    public static Task<string?> OneProxyCurrentAsync(LlmRouterDbContext db, JsonElement sdata) =>
        Cache.GetOrAddAsync("oneproxy", TimeSpan.FromSeconds(10),
            () => Routing.RoutingOps.OneProxyCurrentAsync(db, sdata));

    /// <summary>Skills system-prompt fragment — was a recursive directory scan
    /// per request. Factory keeps ASP.NET types out of Core.</summary>
    public static Task<string?> SkillsPromptAsync(
        LlmRouterDbContext db, Func<Task<string?>> factory) =>
        Cache.GetOrAddAsync("skills:prompt", TimeSpan.FromSeconds(60), factory);

    /// <summary>Auto-combo candidate pool — short TTL since it reflects
    /// connection availability.</summary>
    public static Task<List<string>> AutoPoolAsync(
        LlmRouterDbContext db, Registry.ProviderRegistry registry, string spec) =>
        Cache.GetOrAddAsync($"autopool:{spec}", TimeSpan.FromSeconds(5),
            () => Routing.AutoCombos.ResolveCandidatesAsync(db, registry, spec, CancellationToken.None));

    // === SPEC-163: remaining per-request reads moved behind the cache ===

    /// <summary>kv scope for model aliases (mirrors MgmtBatchEndpoints.AliasScope).</summary>
    public const string AliasScope = "aliasModels";

    /// <summary>Model alias → provider/model target. Was an uncached
    /// `db.Kv.FindAsync` per request; alias writes bust the whole cache.</summary>
    public static Task<string> AliasAsync(LlmRouterDbContext db, string model) =>
        Cache.GetOrAddAsync($"alias:{model}", TimeSpan.FromSeconds(30), async () =>
        {
            var row = await db.Kv.AsNoTracking()
                .FirstOrDefaultAsync(k => k.Scope == AliasScope && k.Key == model);
            if (row is null) return model;
            try
            {
                var d = JsonDocument.Parse(row.Value).RootElement;
                return d.TryGetProperty("target", out var t) ? t.GetString() ?? model : model;
            }
            catch (JsonException) { return model; }
        });

    /// <summary>User row by id — vk auth + free-plan checks used to query
    /// `db.Users` per request. User writes bust the whole cache.</summary>
    public static Task<User?> UserAsync(LlmRouterDbContext db, string uid) =>
        Cache.GetOrAddAsync($"user:{uid}", TimeSpan.FromSeconds(15), () =>
            db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid));

    /// <summary>Enabled budgets — was `db.Budgets.ToListAsync` per request
    /// in BudgetGate. Budget writes bust the whole cache.</summary>
    public static Task<List<Budget>> EnabledBudgetsAsync(LlmRouterDbContext db) =>
        Cache.GetOrAddAsync("budgets:enabled", TimeSpan.FromSeconds(15), () =>
            db.Budgets.AsNoTracking().Where(b => b.Enabled).ToListAsync());

    /// <summary>Cached budget spend verdict — the SUM over usageHistory used to
    /// run per request per applicable budget. 5s TTL keeps the cap near-exact;
    /// usage writes invalidate the "spend" tag alongside "dcap".</summary>
    public static Task<(double Usd, long Tokens)> BudgetSpendAsync(
        LlmRouterDbContext db, Data.Budget b, string since) =>
        Cache.GetOrAddAsync($"spend:{b.Id}:{b.Scope}:{since}", TimeSpan.FromSeconds(5),
            () => Budgets.BudgetGate.SpendAsync(db, b, since), ["spend"]);

    /// <summary>Free-plan daily request count — kv counter `usage:free:{uid}:{date}`
    /// bumped by UsageSideEffects inside the usage batch. Falls back to a
    /// one-time COUNT over usageHistory and seeds the counter on first read
    /// after deploy.</summary>
    public static async Task<int> FreeDailyUsedAsync(LlmRouterDbContext db, string uid)
    {
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var key = $"free:{uid}:{today}";
        var row = await db.Kv.FindAsync("usage", key);
        if (row is not null && int.TryParse(row.Value, out var n)) return n;
        var used = await db.UsageHistory.CountAsync(
            u => u.UserId == uid && string.Compare(u.Timestamp, today) >= 0);
        if (row is null) db.Kv.Add(new Data.KvEntry { Scope = "usage", Key = key, Value = used.ToString() });
        else row.Value = used.ToString();
        await db.SaveChangesAsync();
        return used;
    }

    /// <summary>Bump the free-plan daily counter inside the writer's serial
    /// transaction (telemetry kv scope — doesn't bust the config cache).</summary>
    public static async Task CreditFreeDailyAsync(LlmRouterDbContext db, string uid)
    {
        var key = $"free:{uid}:{DateTime.UtcNow.Date:yyyy-MM-dd}";
        var row = await db.Kv.FindAsync("usage", key);
        if (row is null)
            db.Kv.Add(new Data.KvEntry { Scope = "usage", Key = key, Value = "1" });
        else
            row.Value = (int.TryParse(row.Value, out var n) ? n + 1 : 1).ToString();
    }
}
