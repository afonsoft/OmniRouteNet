// System Health & Diagnostics Aggregator for OmniRoute Parity
using System.Collections.Generic;
using LLMRouter.Core.Diagnostics;
using LLMRouter.Core.Enterprise;

namespace LLMRouter.Core.Health
{
    public static class SystemHealthAggregator
    {
        public static Dictionary<string, object> GetSystemStatus()
        {
            return new Dictionary<string, object>
            {
                { "status", "healthy" },
                { "hotPathMicrosUpTime", HotPathDiagnostics.GetElapsedMicroseconds() },
                { "enterprisePoliciesLoaded", true },
                { "version", "3.8.54-delta" }
            };
        }
    }
}
