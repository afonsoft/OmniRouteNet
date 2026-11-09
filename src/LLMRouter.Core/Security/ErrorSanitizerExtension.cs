// ErrorSanitizer enhancement for v3.8.54 delta upstream alignment
using System;

namespace LLMRouter.Core.Security
{
    public static class ErrorSanitizerExtension
    {
        public static string SanitizeUpstreamError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return errorMessage;
            // Redact potential API keys or tokens in upstream error strings
            return errorMessage
                .Replace("sk-proj-", "sk-redacted-")
                .Replace("sk-", "sk-redacted-");
        }
    }
}
