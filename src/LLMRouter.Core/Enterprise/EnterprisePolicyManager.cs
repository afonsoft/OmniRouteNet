// SPEC-169 & SPEC-170: Teams, Cost Centers and Enterprise Entra SSO / MFA policies
using System.Collections.Generic;

namespace LLMRouter.Core.Enterprise
{
    public class TeamCostCenterPolicy
    {
        public string TeamId { get; set; } = string.Empty;
        public string CostCenterCode { get; set; } = string.Empty;
        public decimal MonthlyBudgetLimit { get; set; } = 1000.0m;
        public bool MfaEnforced { get; set; } = true;
    }

    public static class EnterprisePolicyManager
    {
        private static readonly Dictionary<string, TeamCostCenterPolicy> _policies = new();

        public static void RegisterPolicy(TeamCostCenterPolicy policy)
        {
            _policies[policy.TeamId] = policy;
        }

        public static TeamCostCenterPolicy? GetPolicy(string teamId)
        {
            _policies.TryGetValue(teamId, out var policy);
            return policy;
        }
    }
}
