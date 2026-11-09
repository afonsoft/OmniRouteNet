# OmniRoute → LLMRouter gap classification — pin → tag v3.8.53

## Método

- **Pin novo (alvo da migração):** tag `v3.8.53` = `191dce8d534ceb56c28a08e71381284a9a87838f` (2026-11-06), `github.com/cheaper-inference/OmniRoute`. O tip da branch `release/v3.8.53` (`6cc69650`) tem a mesma árvore da tag.
- **Pin antigo (substituído):** `412319f0bdda06a1c7db93dd62bdf8b6317edc1e` (2026-10-26, branch `release/v3.8.53` da época).
- **Fato crítico:** o pin antigo **não é ancestral** da tag — o histórico do upstream foi reescrito (merge-base `edab7c3a`). Toda comparação upstream é **por árvore** (`git diff <a> <b>`), nunca `git rev-list`/`git log <a>..<b>`.
- **Tamanho do delta pin→tag (por árvore):** 6.358 arquivos no total; **1.868 de código** (excluindo docs/tests/changelog.d); 2.971 são só docs/i18n.
- **v3.8.54:** branch default do upstream, **sem tag** e em movimento — fora de escopo, exceto os cherry-picks **#19123** e **#19109** (ambos na SPEC-165). O pin sobe para a tag v3.8.54 quando ela for publicada.
- **9router:** pin mantido em `ce4460ef79382bfddb4aa5fc0ff9f3cb0d5f95a8` (HEAD atual `1fe586bb` fora de escopo).

Legenda de status: `ported` = já existe no LLMRouter · `partial` = começado, delta documentado · `missing` = não existe em `src/` · `out-of-scope` = decisão explícita (motivo na nota).

## Delta upstream pin→tag (v3.8.53)

Fonte: SPEC aprovada `7846f2c8-1b49-4d73-a519-5a996e0b85ed` §3.3 (cada item verificado com 0 ocorrências em `src/`).

| Item | Área | Status | SPEC | Notas |
|---|---|---|---|---|
| `open-sse/services/combo/{agenticStall,agenticStallFailover,attemptFailure,authorizedRetryWait,comboServiceTier,mediaCompatibility,preContentTransportFailure,truncatedStreamFailover}.ts` | combo/resilience runtime | missing | SPEC-165 | Port para `Core/Routing`/`Core/Resilience` |
| `accountFallback/antiAvalanche.ts`, `codexAccount/capacity429.ts`, `antigravity429Guard.ts` | combo/resilience runtime | missing | SPEC-165 | Anti-avalanche + guards 429 |
| `assistantPrefill.ts`, `assistantPrefillError.ts`, `learnedMaxTokensCaps.ts` | combo/resilience runtime | missing | SPEC-165 | Prefill + caps aprendidos |
| `codexQuota{Parser,Signals,WindowPolicy}.ts`, `githubCreditLimit.ts`, `githubCopilotCapabilities.ts`, `tokenRefresh/passiveCodex.ts` | combo/resilience runtime | missing | SPEC-165 | Quota/retry por provider |
| `src/sse/services/{ciPayloadQuotaFailure,comboTimeoutSessionAffinity,expiryFirstMetrics}.ts` | combo/resilience runtime | missing | SPEC-165 | |
| handlers `chatCore/{clientSessionId,nonStreamingJsonDocument,searchProviderRejection,streamStateBody,streamingRoutingEvent,turnExecutionContext}.ts` | gateway handlers | missing | SPEC-165 | |
| handlers `sseParser/{responsesSnapshot,truncation}.ts`, `videoGeneration/abort.ts`, `imageResult.ts` | gateway handlers | missing | SPEC-165 | |
| translator `helpers/{mediaContentParts,mediaIntegrity,responsesToolNameRestore}.ts` | translators | missing | SPEC-165 | |
| Cherry-pick #19123 — retire unreported quota windows | combo/resilience (v3.8.54) | missing | SPEC-165 | → `QuotaWindows.cs`/`QuotaSnapshots.cs` |
| Cherry-pick #19109 — protected feature flags exigem mgmt auth mesmo com login off | segurança (v3.8.54) | missing | SPEC-165 | |
| #13102 api-key preferred connections | routing | missing | SPEC-166 | |
| #16737 p2c latency signal | routing | missing | SPEC-166 | |
| #16572 quota-weighted pace scoring | routing | missing | SPEC-166 | |
| #16861 tool disclosure BM25 | routing | missing | SPEC-166 | |
| #16385/#16421 refusal signal/memory | routing | missing | SPEC-166 | |
| #15923 combo stale model badge | routing/UI | missing | SPEC-166 | |
| #16224 `/v1/models` publish auto-lkgp | routing | missing | SPEC-166 | |
| #16247 models-health cooldown route | routing | missing | SPEC-166 | |
| #17140 custom max-output override | routing | missing | SPEC-166 | |
| #16800 inferred free signals | routing | missing | SPEC-166 | |
| #17074 egress quota rotation | routing | missing | SPEC-166 | |
| Executors `muse-code`, `zcode/{gateway,nativeProtocol,profileAuth}`, `geminiLiveTranscribe` | providers/executors | missing | SPEC-167 | |
| Executors `default/{forceStreamBody,museCode,perplexityAgentDefaults}`, `opencode{FreeTierEgressProbe,MuseSparkEffort,ResponsesFields}`, `commandCode/zdr`, `base/maxTokensRecovery`, `codex/ssePeekFailure`, `gemini-web/browserErrors` | providers/executors | missing | SPEC-167 | |
| Handlers `grokMedia`, `systemOne`, `search/kimiSearch` | providers/executors | missing | SPEC-167 | `v1/systemone` já existe no LLMRouter |
| Providers novos #13862 ainetcafe, #16325 onomeo, #16869 bedrock embeddings, #17287 elevenlabs sound/music, #17840 chatgpt-web gpt6, #16348 stepfun balance, #17965 nimble-lite, #17345 opencode-zen free; usage `museCode`, `tokenrouter` | provider registry | missing | SPEC-167 | `providers.json` (hoje 289) |
| Blocos `oauth` para `codebuddy-cn, codebuddy-intl, cursor, devin-desktop, freebuff, muse-code, trae, zed-hosted` + entradas novas `grok-cli-oauth`, `zed` | OAuth registry | missing | SPEC-167 | Hoje 18 providers com bloco `oauth` |
| EF equivalents das migrations `214_call_logs_client_session_id`, `217_quota_recovery_tickets`, `219_semantic_cache_expires_at_index`, `220_compression_combos_output_styles`, `223_call_logs_queue_wait_ms`, `225_token_ledger_balance_indexes` | dados (SQLite/EF) | missing | SPEC-168 | |
| Rotas `combos/export`, `combos/import`, `monitoring/model-health`, `pricing/unpriced-usage`, `usage/availability`, `v1/quota-recoveries/[id]`, `oauth/[provider]/import-export`, `settings/cli-versions`, `system/metrics`, `v0/management/{api-call,auth-files,reset-quota}`, `cli-tools/whycodes-settings` | management/gateway API | missing | SPEC-168 | Shapes vindos dos `route.ts` da tag |
| Migrations 215/216, `src/lib/db/teams.ts`, `teamUsageAnalytics.ts`, `usage/teamCostShapes.ts`, `usage/teamUsageLimits.ts`, `api/teams/**` (4 rotas), página `costs/teams` (11 componentes), doc `TEAM_COST_CENTERS.md` | teams/cost centers | missing | SPEC-169 | `ApiKey.TeamId` existe (`Entities.cs`), sem entidade Team |
| `api/auth/mfa/{setup,enable,verify,disable,status}`, `api/auth/sso/config`, `lib/auth/totp.ts`, `lib/db/dashboardMfa.ts`, migration 222 `entra_sso_identities`, #18001 OIDC role/group admission, `settings/components/MfaSection.tsx` | auth (MFA + Entra SSO) | missing | SPEC-170 | `OidcSubject/OidcIssuer` existem, sem TOTP |
| Rota `/dashboard` sem `@page` equivalente | UI | missing | Onda 1 (RF-019) | Resolve para a home |
| Rota `/dashboard/api-manager/routing` sem `@page` equivalente | UI | missing | Onda 1 (RF-019) | |
| Rota `/dashboard/media-providers/[kind]` sem `@page` equivalente | UI | missing | Onda 1 (RF-019) | |
| Rota `/dashboard/costs/teams` sem `@page` equivalente | UI | missing | SPEC-169 | Vem com o RF-017 |
| 61 chaves i18n faltantes em pt-BR/es (en tem 14.134; pt-BR/es 14.073) | i18n | partial | Onda 1 (RF-020) | + teste de igualdade de chaves |
| sing-box (8 rotas, migration 221) | infra/proxy | out-of-scope | — | Desktop/infra fora do escopo do port docker |
| mihomo/core-config renderer | infra/proxy | out-of-scope | — | Ídem |
| Proxy subscription mirrors/egress families (218/224) | infra/proxy | out-of-scope | — | Ídem |
| Tray preferences | desktop | out-of-scope | — | Electron fora de escopo |
| Bifrost sidecar | infra | out-of-scope | — | Ídem |
| Electron/MITM desktop | desktop | out-of-scope | — | Já fora de escopo desde o início da migração |
| Plugins `@omniroute/opencode-plugin-v2` | ecossistema | out-of-scope | — | Específico do runtime Node do upstream |

## Backlog interno reconciliado (estado do repo local @ main 9d18a751)

| Item | Área | Status | SPEC | Notas |
|---|---|---|---|---|
| Budget overage | budgets | ported | SPEC-150 | PR #194, commit 943922a |
| Perf hot path (cache de config, telemetria off-request) | performance | ported | SPEC-163 | PR #202 |
| Perf stream/body CPU (TrackUsage gated, SSE coalescido) | performance | ported | SPEC-164 | PR #203 |
| CLI device flow (`/api/cli/connect`, `/api/cli/tokens/{token}/approve\|revoke`, `/api/cli/whoami` + `CliDeviceTests` 4 testes) | auth/CLI | ported | SPEC-050 | Base para o device-code |
| Device-code RFC 8628 (`POST /api/cli/device`, `POST /api/cli/device/token`, tabela `cliDeviceCodes`, página `/cli/device?code=`) | auth/CLI | partial | SPEC-154 | Só falta o delta; rotas SPEC-050 continuam |
| Test-mode API (`/api/test/{reset,seed,clock,fail}`, flag `LLMROUTER_TEST_MODE`) | testes | missing | SPEC-155 | |
| Log/audit sinks (bounded channel, NDJSON em lote, retry/backoff, drop-oldest) | observabilidade | missing | SPEC-156 | |
| SBOM + provenance/attestation no `release.yml` | release | missing | SPEC-157 | `ci.yml` intocado |
| Release manifest (`docs/release-manifest.json` + validador xUnit) | release | missing | SPEC-158 | Arquivo ausente no repo |
| Provider operations parity (batch test, agrupamento, configured-only) | providers UI/API | missing | SPEC-159 | Só chaves i18n, sem código |
| Routing decision observability (attempt ledger + UI) | observabilidade | missing | SPEC-160 | Só chaves i18n, sem endpoint |
| UI-parity fixture (`tests/fixtures/ui-parity.json`) | testes | missing | SPEC-161 | `tests/fixtures/` ausente |
| Compat/upgrade validation (schema versioning, dry-run) | release | missing | SPEC-162 | |
| Reconciliação de specs/docs | docs | ported | SPEC-145 | Fechada por este documento + rebaseline do pin |

## Histórico do pin

| Data | Pin | Observação |
|---|---|---|
| 2026-10-26 | `412319f0bdda06a1c7db93dd62bdf8b6317edc1e` | Substituído — branch `release/v3.8.53` da época; não é ancestral da tag (histórico reescrito) |
| 2026-11-06 | `191dce8d534ceb56c28a08e71381284a9a87838f` (tag `v3.8.53`) | **Pin atual** — árvore idêntica ao tip `6cc69650` de `release/v3.8.53` |
| quando a tag v3.8.54 sair | tag `v3.8.54` | PR só de docs; até lá entram só os cherry-picks #19123/#19109 |
