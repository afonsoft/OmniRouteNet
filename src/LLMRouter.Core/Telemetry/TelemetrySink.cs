// SPEC-156: Extended telemetry sinks
using System;
using System.Threading.Tasks;

namespace LLMRouter.Core.Telemetry
{
    public interface ITelemetrySink
    {
        Task EmitAsync(string eventName, object payload);
    }

    public class ConsoleTelemetrySink : ITelemetrySink
    {
        public Task EmitAsync(string eventName, object payload)
        {
            Console.WriteLine($"[TelemetrySink] {eventName}: {payload}");
            return Task.CompletedTask;
        }
    }
}
