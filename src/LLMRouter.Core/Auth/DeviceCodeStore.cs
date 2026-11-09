// SPEC-154: Device Code Store & Endpoints
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using LLMRouter.Core.Auth;

namespace LLMRouter.Core.Auth
{
    public class DeviceCodeSession
    {
        public string DeviceCode { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, approved, denied, expired
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(15);
    }

    public static class DeviceCodeStore
    {
        private static readonly ConcurrentDictionary<string, DeviceCodeSession> _sessions = new();

        public static void Register(DeviceCodeSession session)
        {
            _sessions[session.DeviceCode] = session;
            _sessions[session.UserCode] = session;
        }

        public static DeviceCodeSession? GetByDeviceCode(string deviceCode)
        {
            _sessions.TryGetValue(deviceCode, out var session);
            return session;
        }

        public static DeviceCodeSession? GetByUserCode(string userCode)
        {
            _sessions.TryGetValue(userCode, out var session);
            return session;
        }
    }
}

namespace LLMRouter.Server.Endpoints
{
    public static class DeviceCodeEndpoints
    {
        public static void MapDeviceCodeEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/oauth/device/code", async (HttpContext ctx, DeviceCodeRequest req) =>
            {
                var deviceCode = Guid.NewGuid().ToString();
                var userCode = "WD-" + new Random().Next(1000, 9999);
                
                var session = new DeviceCodeSession
                {
                    DeviceCode = deviceCode,
                    UserCode = userCode,
                    ClientId = req.ClientId ?? "default",
                    Scope = req.Scope ?? ""
                };

                DeviceCodeStore.Register(session);

                var resp = new DeviceCodeResponse
                {
                    DeviceCode = deviceCode,
                    UserCode = userCode,
                    VerificationUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}/device",
                    ExpiresIn = 900,
                    Interval = 5
                };

                return Results.Ok(resp);
            });

            endpoints.MapPost("/oauth/device/token", async (HttpContext ctx) =>
            {
                // Simple polling handler stub for RFC 8628 token endpoint
                return Results.Ok(new { access_token = "sk-device-placeholder", token_type = "Bearer", scope = "" });
            });
        }
    }
}
