# SPEC-168 — Data & API surface v3.8.53

Origin: OmniRoute `v3.8.53` delta vs pin `412319f0` — `src/lib/db/migrations/214..225` (subset) and new `src/app/api/**/route.ts`.

## Goal

Mirror the data-model additions and the new management/API routes.

## Scope

- EF migrations equivalent to upstream 214 (call_logs.client_session_id), 217 (quota_recovery_tickets), 219 (semantic_cache expires_at index), 220 (compression_combos output_styles), 223 (call_logs.queue_wait_ms), 225 (token_ledger balance indexes).
- Routes: `combos/export`, `combos/import`, `monitoring/model-health`, `pricing/unpriced-usage`, `usage/availability`, `v1/quota-recoveries/[id]`, `oauth/[provider]/import-export`, `settings/cli-versions`, `system/metrics`, `v0/management/{api-call,auth-files,reset-quota}`, `cli-tools/whycodes-settings` (`v1/systemone` already ported).
- Request/response shapes copied from the `route.ts` files at `191dce8d`.

## Acceptance criteria

- Each route answers with the same contract as upstream (status codes, payload shapes).
- Schema upgrades apply cleanly on top of current LLMRouter DBs (SPEC-162 covers regression).
- Out-of-scope migrations (sing-box 221, proxy 218/224) are excluded and documented in the gap doc.

## Tests

- Endpoint contract tests per route tagged `# Covers RF-016`.
- Migration up/down tests.