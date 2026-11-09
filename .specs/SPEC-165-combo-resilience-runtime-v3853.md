# SPEC-165 — Combo/resilience runtime v3.8.53 + cherry-picks v3.8.54

Origin: OmniRoute `v3.8.53` (`191dce8d`) delta vs pin `412319f0` — `open-sse/services/combo/*`, `accountFallback/*`, `codexAccount/*`, handlers/translator; plus v3.8.54 cherry-picks #19123 and #19109.

## Goal

Port the v3.8.53 combo/resilience runtime additions 1:1, and pull forward the two v3.8.54 fixes that touch security and routing correctness.

## Scope

- Combo services: agenticStall(+Failover), attemptFailure, authorizedRetryWait, comboServiceTier, mediaCompatibility, preContentTransportFailure, truncatedStreamFailover.
- Resilience: accountFallback/antiAvalanche, codexAccount/capacity429, antigravity429Guard, assistantPrefill(+Error), learnedMaxTokensCaps, codexQuota{Parser,Signals,WindowPolicy}, githubCreditLimit, tokenRefresh/passiveCodex, ciPayloadQuotaFailure, comboTimeoutSessionAffinity, expiryFirstMetrics.
- Handlers: chatCore/{clientSessionId,nonStreamingJsonDocument,searchProviderRejection,streamStateBody,streamingRoutingEvent,turnExecutionContext}, sseParser/{responsesSnapshot,truncation}, videoGeneration/abort, imageResult.
- Translator: helpers/{mediaContentParts,mediaIntegrity,responsesToolNameRestore}.
- **Cherry-pick #19123 (RF-013a)**: retire unreported quota windows — a *complete* usage reading that no longer carries a previously persisted window (5h/7d) retires it (no more QUOTA_EXHAUSTED/cooldown from it); partial readings never retire. Local: `Routing/QuotaWindows.cs`, `Routing/QuotaSnapshots.cs`.
- **Cherry-pick #19109 (RF-013b)**: protected feature flags — mutating a flag on the protected list requires management auth *even with dashboard login disabled*; out-of-enum value → 400 listing allowed values. Local: feature-flag endpoints in `SettingsRoutingEndpoints.cs`/`AuditOpsEndpoints.cs`.

## Acceptance criteria

- AC-13: persisted 7d window marked exhausted + next complete reading carries only 5h → 7d retired, connection routable again; incomplete reading → nothing retired.
- AC-14: dashboard login disabled + PUT on a protected flag without mgmt auth → 401/403; out-of-enum value → 400.
- Truncated stream before first content in a combo step fails over to the next step.
- Each ported service keeps upstream semantics; test derived from the upstream `*.test.ts` where one exists.

## Tests

- Unit tests per service mirroring upstream test files (tags `# Covers RF-013` / `RF-013a` / `RF-013b`).
- Quota-window retire: complete/incomplete/still-reported matrix.
- Protected-flags auth matrix: login on/off × protected/non-protected × valid/invalid enum.