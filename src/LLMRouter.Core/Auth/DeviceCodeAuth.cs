// SPEC-154: Device Code Flow (RFC 8628) support for upstream parity
using System;
using System.Text.Json.Serialization;

namespace LLMRouter.Core.Auth
{
    public class DeviceCodeRequest
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    public class DeviceCodeResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("user_code")]
        public string UserCode { get; set; } = "WD-1234";

        [JsonPropertyName("verification_uri")]
        public string VerificationUri { get; set; } = "https://llmrouter.local/device";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 900;

        [JsonPropertyName("interval")]
        public int Interval { get; set; } = 5;
    }
}
