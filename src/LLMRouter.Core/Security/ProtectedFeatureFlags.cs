namespace LLMRouter.Core.Security;

/// <summary>
/// SPEC-165 / upstream #19109 (protectedFeatureFlags.ts): flags that loosen an
/// access, SSRF, TLS or sandbox control when flipped. Writing a DB override
/// for one of these requires a real management credential even when dashboard
/// login is disabled (requireLogin=false) — an anonymous dashboard session
/// must not be able to switch the control off.
/// </summary>
public static class ProtectedFeatureFlags
{
    /// <summary>
    /// Flags outside the security category that still loosen an access, SSRF,
    /// TLS or sandbox control when flipped. Every security-category flag is
    /// covered below, so the set is explicit here.
    /// </summary>
    public static readonly HashSet<string> Protected = new(StringComparer.OrdinalIgnoreCase)
    {
        // security category (upstream FEATURE_FLAG_DEFINITIONS)
        "REQUIRE_API_KEY",
        "INPUT_SANITIZER_ENABLED",
        "INJECTION_GUARD_MODE",
        "API_KEY_AUDIT_CLIENT_DETAILS_ENABLED",
        "PII_REDACTION_ENABLED",
        "PII_RESPONSE_SANITIZATION",
        "PII_RESPONSE_SANITIZATION_MODE",
        "OUTBOUND_SSRF_GUARD_ENABLED",
        "ALLOW_API_KEY_REVEAL",
        "AUTH_LOG_INCLUDE_ACCOUNT_ID",
        "OMNIROUTE_OIDC_DISABLE_PASSWORD_LOGIN",
        // outside the security category but control-loosening (upstream list)
        "OMNIROUTE_ALLOW_PRIVATE_PROVIDER_URLS",
        "OMNIROUTE_ALLOW_LOCAL_PROVIDER_URLS",
        "MITM_DISABLE_TLS_VERIFY",
        "OMNIROUTE_MCP_ENFORCE_SCOPES",
        "SKILLS_SANDBOX_NETWORK_ENABLED",
    };

    /// <summary>Enum-restricted flags (upstream enumValues) — anything else is a free-form string.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> EnumValues =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["INJECTION_GUARD_MODE"] = ["off", "warn", "block", "redact"],
            ["PII_RESPONSE_SANITIZATION_MODE"] = ["redact", "warn", "block", "off"],
        };

    public static bool IsProtected(string key) => Protected.Contains(key);

    /// <summary>Validate a proposed value; returns an error message or null when ok.
    /// A null value removes the override (still gated by the credential check).</summary>
    public static string? ValidateValue(string key, string? value)
    {
        if (!EnumValues.TryGetValue(key, out var allowed)) return null;
        if (value is null || allowed.Contains(value, StringComparer.Ordinal)) return null;
        return $"Invalid value \"{value}\" for enum flag {key}. Allowed: {string.Join(", ", allowed)}";
    }
}
