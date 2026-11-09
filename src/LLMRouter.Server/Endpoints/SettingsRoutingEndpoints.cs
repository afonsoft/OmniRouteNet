using System.Text.Json;
using System.Text.Json.Nodes;
using LLMRouter.Core.Data;
using LLMRouter.Core.Jobs;
using LLMRouter.Core.Routing;
using LLMRouter.Core.Usage;
using Microsoft.EntityFrameworkCore;

namespace LLMRouter.Server.Endpoints;

/// <summary>SPEC-047: ip-filter, payload-rules, reasoning-routing (+simulate),
/// task-routing, free-proxies fetch, oneproxy.</summary>
public static class SettingsRoutingEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static async Task<JsonObject> SettingsNode(LlmRouterDbContext db)
    {
        var s = await db.Settings.FirstOrDefaultAsync();
        var d = AuthEndpoints.Parse(s?.Data);
        return d.ValueKind == JsonValueKind.Object ? (JsonNode.Parse(d.GetRawText()) as JsonObject) ?? [] : [];
    }

    private static async Task SaveSection(LlmRouterDbContext db, string key, JsonNode? value)
    {
        var s = await Core.Extras.Extras.GetOrCreateSettingsAsync(db);
        var d = AuthEndpoints.Parse(s.Data).Deserialize<Dictionary<string, JsonElement>>() ?? [];
        d[key] = JsonSerializer.SerializeToElement(value);
        s.Data = JsonSerializer.Serialize(d);
        await db.SaveChangesAsync();
        await Core.Extras.Extras.AuditAsync(db, $"settings.{key}", "update");
    }

    /// <summary>
    /// SPEC-165 / #19109: a "real credential" = dashboard session backed by a
    /// password (claim cred=pwd) or a valid gateway api key — never an
    /// anonymous session created by requireLogin=false.
    /// </summary>
    private static async Task<bool> HasManagementCredential(HttpContext ctx, LlmRouterDbContext db)
    {
        if (ctx.User.Claims.Any(c => c.Type == "cred" && (c.Value == "pwd" || c.Value == "apikey"))) return true;
        var auth = await GatewayEndpoints.AuthenticatedKey(ctx, db);
        return auth.Authed && auth.Key != "dashboard";
    }

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api").RequireAuthorization();

        // generic get/put for each routing section
        foreach (var (key, seg) in new[]
        {
            ("ipFilter", "ip-filter"), ("payloadRules", "payload-rules"),
            ("reasoningRoutingRules", "reasoning-routing-rules"),
            ("taskRouting", "task-routing"), ("oneproxy", "oneproxy"),
            ("freeProxies", "free-proxies"),
            // SPEC-096: remaining upstream sub-settings (same sdata-key shape)
            ("featureFlags", "feature-flags"), ("systemPrompt", "system-prompt"),
            ("thinkingBudget", "thinking-budget"), ("tierConfig", "tier-config"),
            ("autoDisableAccounts", "auto-disable-accounts"),
            ("backgroundDegradation", "background-degradation"),
            ("cacheConfig", "cache-config"), ("database", "database"),
        })
        {
            g.MapGet($"/settings/{seg}", async (LlmRouterDbContext db) =>
                Results.Json(new { value = (await SettingsNode(db))[key]?.DeepClone() },
                    JsonOpts));

            g.MapPut($"/settings/{seg}", async (HttpContext ctx, LlmRouterDbContext db) =>
            {
                var req = await JsonSerializer.DeserializeAsync<JsonNode>(ctx.Request.Body);
                var val = req is JsonObject o && o.ContainsKey("value") ? o["value"] : req;

                // SPEC-165 / upstream #19109: protected feature flags demand a
                // real management credential (password-backed session or a
                // gateway api key) even when dashboard login is disabled.
                if (key == "featureFlags" && val is JsonObject rawFlags)
                {
                    // Normalize keys: match case-insensitively against protected/known flags
                    var flags = new JsonObject();
                    foreach (var (k, v) in rawFlags)
                    {
                        var canonical = Core.Security.ProtectedFeatureFlags.Protected
                            .FirstOrDefault(p => string.Equals(p, k, StringComparison.OrdinalIgnoreCase)) ?? k;
                        flags[canonical] = v?.DeepClone();
                    }
                    val = flags;

                    foreach (var kv in flags)
                    {
                        string? sv;
                        if (kv.Value is null) sv = null;
                        else if (kv.Value is JsonValue jv && jv.TryGetValue<string>(out var s)) sv = s;
                        else sv = kv.Value.ToJsonString();
                        var err = Core.Security.ProtectedFeatureFlags.ValidateValue(kv.Key, sv);
                        if (err is not null)
                            return Results.Json(new { error = err }, JsonOpts, statusCode: 400);
                    }

                    var touched = flags.Where(kv =>
                        Core.Security.ProtectedFeatureFlags.IsProtected(kv.Key)).ToList();
                    if (touched.Count > 0 && !await HasManagementCredential(ctx, db))
                        return Results.Json(new
                        {
                            error = "management_credential_required",
                            flags = touched.Select(t => t.Key),
                        }, JsonOpts, statusCode: 403);
                }

                await SaveSection(db, key, val);
                return Results.Json(new { success = true }, JsonOpts);
            });
        }

        // reasoning simulate — returns the matched rule + resulting body without dispatch
        g.MapPost("/settings/reasoning-rules/simulate", async (HttpContext ctx, LlmRouterDbContext db) =>
        {
            var req = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
            var model = req.TryGetProperty("model", out var m) ? m.GetString() ?? "" : "";
            var body = req.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.Object
                ? b : JsonSerializer.SerializeToElement(new { model });
            var sdata = await PricingService.SettingsDataAsync(db);
            var (rewritten, pattern, effort) =
                RoutingOps.ApplyReasoningRules(sdata, body, model);
            return Results.Json(new { matched = pattern, effort, body = rewritten }, JsonOpts);
        });

        // task routing resolve
        g.MapPost("/settings/task-routing/resolve", async (HttpContext ctx, LlmRouterDbContext db) =>
        {
            var req = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
            var task = req.TryGetProperty("taskType", out var t) ? t.GetString() ?? "" : "";
            var sdata = await PricingService.SettingsDataAsync(db);
            var model = RoutingOps.ResolveTaskModel(sdata, task);
            return Results.Json(new { taskType = task, model }, JsonOpts);
        });

        // free proxies — fetch a list source or accept explicit entries, then
        // (re)populate the "free-proxies" pool and health-check it
        g.MapPost("/proxy-pools/fetch-free", async (HttpContext ctx, LlmRouterDbContext db,
            IHttpClientFactory hf) =>
        {
            var req = ctx.Request.ContentLength > 0
                ? await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body)
                : (JsonElement?)null;
            var entries = req is { } r && r.TryGetProperty("entries", out var e)
                && e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList()
                : null;
            List<string> urls;
            if (entries is { Count: > 0 })
                urls = entries.Select(x => x.StartsWith("http") ? x : $"http://{x}").ToList();
            else
            {
                var source = req is { } r2 && r2.TryGetProperty("source", out var so)
                    ? so.GetString() : null;
                if (string.IsNullOrEmpty(source))
                    return Results.Json(new { error = "provide 'entries' or 'source'" },
                        JsonOpts, statusCode: 400);
                var text = await hf.CreateClient().GetStringAsync(source, ctx.RequestAborted);
                urls = RoutingOps.ParseFreeProxies(text);
            }

            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var pool = await db.ProxyPools.FirstOrDefaultAsync(p => p.Id == "free-proxies")
                ?? db.ProxyPools.Add(new ProxyPool
                { Id = "free-proxies", CreatedAt = now, UpdatedAt = now, Data = "{}" }).Entity;
            pool.Data = JsonSerializer.Serialize(new
            {
                name = "free-proxies",
                proxies = urls.Select(u => new { url = u, ok = false, failCount = 0 }).ToArray(),
                probeUrl = "https://api.ipify.org/?format=json",
            });
            pool.UpdatedAt = now;
            pool.IsActive = true;
            await db.SaveChangesAsync();
            var (ok, fail) = await BuiltinJobs.ProxyPoolHealthJob.CheckPoolAsync(db, pool, ctx.RequestAborted);
            await db.SaveChangesAsync();
            return Results.Json(new { pool = "free-proxies", count = urls.Count, ok, fail }, JsonOpts);
        });

        // oneproxy current + manual rotate
        g.MapGet("/proxy/oneproxy", async (LlmRouterDbContext db) =>
        {
            var sdata = await PricingService.SettingsDataAsync(db);
            var cur = await RoutingOps.OneProxyCurrentAsync(db, sdata);
            return Results.Json(new { current = cur }, JsonOpts);
        });

        g.MapPost("/proxy/oneproxy/rotate", async (LlmRouterDbContext db) =>
        {
            var sdata = await PricingService.SettingsDataAsync(db);
            var cur = await RoutingOps.OneProxyRotateAsync(db, sdata);
            return Results.Json(new { current = cur }, JsonOpts);
        });
    }
}
