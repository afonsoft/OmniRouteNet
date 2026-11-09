using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LLMRouter.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LLMRouter.Tests;

/// <summary>SPEC-165 RF-013b (upstream #19109): writing a protected feature
/// flag requires a real management credential even when dashboard login is
/// disabled; out-of-enum values are rejected with 400.</summary>
public class Spec165ProtectedFeatureFlagTests : WebAppTestBase
{
    private async Task SetRequireLogin(bool value)
    {
        using var db = Db();
        var s = await db.Settings.FirstOrDefaultAsync()
            ?? db.Settings.Add(new SettingRow { Data = "{}" }).Entity;
        var d = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(s.Data) ?? [];
        d["requireLogin"] = JsonSerializer.SerializeToElement(value);
        s.Data = JsonSerializer.Serialize(d);
        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> AnonymousSessionClient()
    {
        await SetRequireLogin(false);
        var c = Factory.CreateClient();
        // wrong password, but requireLogin=false mints an anonymous session (no cred claim)
        var r = await c.PostAsJsonAsync("/api/auth/login", new { password = "definitely-wrong" });
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        return c;
    }

    private async Task<HttpClient> PasswordSessionClient()
    {
        var c = Factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/login", new { password = "test1234" });
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        return c;
    }

    // # Covers RF-013b (AC-14: login disabled + no real credential → 403)
    [Fact]
    public async Task Protected_flag_write_without_credential_is_403()
    {
        using var c = await AnonymousSessionClient();
        var r = await c.PutAsJsonAsync("/api/settings/feature-flags",
            new { value = new { REQUIRE_API_KEY = "false" } });
        r.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var body = await r.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetString().ShouldBe("management_credential_required");
    }

    // # Covers RF-013b (out-of-enum value → 400 listing allowed values)
    [Fact]
    public async Task Out_of_enum_value_is_400_with_allowed_list()
    {
        using var c = await PasswordSessionClient();
        var r = await c.PutAsJsonAsync("/api/settings/feature-flags",
            new { value = new { INJECTION_GUARD_MODE = "nonsense" } });
        r.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await r.Content.ReadFromJsonAsync<JsonDocument>();
        var err = body!.RootElement.GetProperty("error").GetString()!;
        err.ShouldContain("INJECTION_GUARD_MODE");
        err.ShouldContain("off, warn, block, redact");
    }

    // # Covers RF-013b (valid credential + valid value → 200)
    [Fact]
    public async Task Protected_flag_write_with_password_session_succeeds()
    {
        using var c = await PasswordSessionClient();
        var r = await c.PutAsJsonAsync("/api/settings/feature-flags",
            new { value = new { INJECTION_GUARD_MODE = "warn" } });
        r.EnsureSuccessStatusCode();
        // persisted
        using var db = Db();
        var s = await db.Settings.FirstAsync();
        var d = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(s.Data)!;
        d["featureFlags"].GetProperty("INJECTION_GUARD_MODE").GetString().ShouldBe("warn");
    }

    // # Covers RF-013b (gateway api key counts as a management credential)
    [Fact]
    public async Task Protected_flag_write_with_gateway_api_key_succeeds()
    {
        using (var db = Db())
        {
            db.ApiKeys.Add(new ApiKey { Id = "k165", Key = "sk-165-flags", IsActive = true });
            await db.SaveChangesAsync();
        }
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", "sk-165-flags");
        var r = await c.PutAsJsonAsync("/api/settings/feature-flags",
            new { value = new { REQUIRE_API_KEY = "true" } });
        r.EnsureSuccessStatusCode();
    }

    // # Covers RF-013b (non-protected flag keeps the old behavior)
    [Fact]
    public async Task Non_protected_flag_write_keeps_compatible_behavior()
    {
        using var c = await AnonymousSessionClient();
        var r = await c.PutAsJsonAsync("/api/settings/feature-flags",
            new { value = new { SOME_FREE_FORM_FLAG = "true" } });
        r.EnsureSuccessStatusCode();
    }

    // # Covers RF-013b (core classification contract)
    [Fact]
    public void Protected_set_matches_upstream_19109()
    {
        Core.Security.ProtectedFeatureFlags.IsProtected("REQUIRE_API_KEY").ShouldBeTrue();
        Core.Security.ProtectedFeatureFlags.IsProtected("OMNIROUTE_ALLOW_PRIVATE_PROVIDER_URLS").ShouldBeTrue();
        Core.Security.ProtectedFeatureFlags.IsProtected("MITM_DISABLE_TLS_VERIFY").ShouldBeTrue();
        Core.Security.ProtectedFeatureFlags.IsProtected("SKILLS_SANDBOX_NETWORK_ENABLED").ShouldBeTrue();
        Core.Security.ProtectedFeatureFlags.IsProtected("SOME_FREE_FORM_FLAG").ShouldBeFalse();
    }
}
