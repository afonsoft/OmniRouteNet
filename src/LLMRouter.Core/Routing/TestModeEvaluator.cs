// SPEC-155: Test-Mode advanced routing evaluator
using System.Collections.Generic;

namespace LLMRouter.Core.Routing
{
    public class TestModeEvaluator
    {
        public static Dictionary<string, object> EvaluateTestMode(string model, string prompt)
        {
            return new Dictionary<string, object>
            {
                { "mode", "test-mode" },
                { "simulatedModel", model },
                { "promptLength", prompt?.Length ?? 0 },
                { "success", true },
                { "estimatedLatencyMs", 42 }
            };
        }
    }
}
