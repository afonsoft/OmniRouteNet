// Final integration validation test stub for OmniRoute parity modules
using Xunit;
using Shouldly;
using LLMRouter.Core.Health;
using LLMRouter.Core.Compression;
using LLMRouter.Core.Enterprise;

namespace LLMRouter.Tests
{
    public class OmniRouteParityFinalTests
    {
        [Fact]
        public void SystemHealthAggregator_Returns_Healthy_Status()
        {
            var status = SystemHealthAggregator.GetSystemStatus();
            status.ShouldContainKey("status");
            status["status"].ShouldBe("healthy");
        }

        [Fact]
        public async Task Compression_Engine_Fail_Open_Works()
        {
            var engine = new FailOpenContextCompressionEngine();
            var result = await engine.CompressAsync("Short test content");
            result.ShouldBe("Short test content");
        }

        [Fact]
        public void Enterprise_Policy_Manager_Stores_Policy()
        {
            var policy = new TeamCostCenterPolicy { TeamId = "team-alpha", CostCenterCode = "CC-999" };
            EnterprisePolicyManager.RegisterPolicy(policy);
            
            var retrieved = EnterprisePolicyManager.GetPolicy("team-alpha");
            retrieved.ShouldNotBeNull();
            retrieved.CostCenterCode.ShouldBe("CC-999");
        }
    }
}
