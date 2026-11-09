// SPEC-159/160: Advanced resilience and routing heuristics (cooldowns & headroom)
using System;
using System.Collections.Concurrent;

namespace LLMRouter.Core.Routing
{
    public class ModelCooldownManager
    {
        private static readonly ConcurrentDictionary<string, DateTime> _cooldowns = new();

        public static void SetCooldown(string modelId, TimeSpan duration)
        {
            _cooldowns[modelId] = DateTime.UtcNow.Add(duration);
        }

        public static bool IsInCooldown(string modelId)
        {
            if (_cooldowns.TryGetValue(modelId, out var expiry))
            {
                if (DateTime.UtcNow < expiry) return true;
                _cooldowns.TryRemove(modelId, out _);
            }
            return false;
        }
    }
}
