// SPEC-163/164: Hot-path performance telemetry & caching optimizations
using System;
using System.Diagnostics;

namespace LLMRouter.Core.Diagnostics
{
    public static class HotPathDiagnostics
    {
        private static readonly Stopwatch _sw = Stopwatch.StartNew();

        public static long GetElapsedMicroseconds()
        {
            return _sw.ElapsedTicks * 1000000 / Stopwatch.Frequency;
        }

        public static void MeasureHotPath(string operationName, Action action)
        {
            var start = GetElapsedMicroseconds();
            try
            {
                action();
            }
            finally
            {
                var elapsed = GetElapsedMicroseconds() - start;
                if (elapsed > 10000) // Log warning if hot-path operation takes > 10ms
                {
                    Console.WriteLine($"[HotPathWarning] Operation {operationName} took {elapsed} us");
                }
            }
        }
    }
}
