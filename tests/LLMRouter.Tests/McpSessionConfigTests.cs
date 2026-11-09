using System;
using Xunit;
using Shouldly;
using LLMRouter.Core.Mcp;

namespace LLMRouter.Tests
{
    public class McpSessionConfigTests
    {
        [Fact]
        public void Default_Idle_Timeout_Is_30_Minutes()
        {
            Environment.SetEnvironmentVariable("OMNIROUTE_MCP_SESSION_IDLE_MS", null);
            var timeout = McpSessionConfig.GetSessionIdleTimeoutMs();
            timeout.ShouldBe(30 * 60 * 1000);
        }

        [Fact]
        public void Custom_Idle_Timeout_From_Environment()
        {
            Environment.SetEnvironmentVariable("OMNIROUTE_MCP_SESSION_IDLE_MS", "60000");
            try
            {
                var timeout = McpSessionConfig.GetSessionIdleTimeoutMs();
                timeout.ShouldBe(60000);
            }
            finally
            {
                Environment.SetEnvironmentVariable("OMNIROUTE_MCP_SESSION_IDLE_MS", null);
            }
        }
    }
}
