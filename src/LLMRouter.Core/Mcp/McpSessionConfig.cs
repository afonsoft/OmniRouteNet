// OMNIROUTE_MCP_SESSION_IDLE_MS support for v3.8.54 delta
using System;

namespace LLMRouter.Core.Mcp
{
    public static class McpSessionConfig
    {
        public static int GetSessionIdleTimeoutMs()
        {
            var envVal = Environment.GetEnvironmentVariable("OMNIROUTE_MCP_SESSION_IDLE_MS");
            if (int.TryParse(envVal, out var parsed) && parsed > 0)
            {
                return parsed;
            }
            // Default 30 minutes upstream
            return 30 * 60 * 1000;
        }
    }
}
