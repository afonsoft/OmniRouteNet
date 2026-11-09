# Plano v2 (revalidado): Fechamento da migração OmniRoute → LLMRouter

## Summary

Revalidação feita em 2026-11-05 contra `origin/main` @ `9d18a751` (último merge: SPEC-164) e um clone novo
de `cheaper-inference/OmniRoute`. O plano v1 tinha erros factuais, corrigidos aqui:

| Afirmação do v1 | Valor verificado |
|---|---|
| Tip de `release/v3.8.53` = `785a3576` | **Errado.** Tip = `6cc69650` (2026-11-05). `785a3576` existe, mas é um commit intermediário |
| — | **Tag `v3.8.53` publicada** = `191dce8d` (2026-11-06); árvore idêntica ao tip de `release/v3.8.53` (diff vazio) |
| — | Branch default do upstream agora é `release/v3.8.54` (`120403bb`), +283 arquivos vs a tag v3.8.53 |
| Drift de 5.812 arquivos | **6.358 arquivos** pin→tag; descontando docs/tests/changelog sobram **1.868 arquivos de código** (2.971 são só `docs/i18n`) |
| SPEC-154..162 sem código | SPEC-154 é **parcial** (o fluxo `/api/cli/connect`+approve+whoami da SPEC-050 existe; o contrato device-code RFC 8628 não). SPEC-150 está **feita** (#194). 155–162 sem código (confirmado) |
| ~25 páginas de dashboard faltando | **4 rotas** após normalizar `[id]`↔`{Id}`; as 13 sub-rotas de settings estão cobertas por `/dashboard/settings/{Section}` |
| 29 providers OAuth faltando | O `OAuthService` é genérico (device/pkce/poll guiado pelo bloco `oauth` do registry; 18 providers já têm o bloco). Faltam **10**: 8 já estão no registry sem bloco `oauth`, 2 nem estão no registry |
| 18 serviços sse novos | **~55 arquivos de runtime novos** em `open-sse/{services,handlers,executors,translator}` + `src/sse/services` |
| i18n 3 locales 100% | en = 14.134 chaves; pt-BR/es = 14.073 → **faltam 61 chaves** em cada |

Estado local: build `0 Error(s)`, 80 warnings; 289 providers; 856 `Map*` em 73 arquivos de endpoint;
162 rotas `@page`; 825 `[Fact|Theory]`. Testes: o testhost passa de 250 s **mesmo com filtro de 3 classes**
(trava na inicialização, não é lentidão da suíte). Validação usada: só o build.

Pin recomendado: **tag `v3.8.53` (`191dce8d534ceb56c28a08e71381284a9a87838f`)** — imutável, e o conteúdo
bate com o tip da branch. Atenção: o pin antigo `412319f0` **não é ancestral** da tag (o histórico foi
reescrito no release), então o diff tem que ser feito por árvore (`git diff 412319f0 v3.8.53`), não por `rev-list`.

## Implementation Steps

### Onda 0 — Corrigir a baseline (docs/SPECs, T1)
1. Atualizar `.specs/README.md` (pin → tag `v3.8.53` / `191dce8d`), `docs/analysis/omniroute-migration-v3.8.53.md`
   e os READMEs. Corrigir os status: SPEC-150/163/164 = done; SPEC-154 = partial; 9router `ce4460ef` → avaliar o HEAD `1fe586bb`.
2. Criar `docs/analysis/omniroute-gap-v3.8.53.md` com o delta por área (seção "Delta verificado" abaixo) e a
   classificação `ported|partial|missing|out-of-scope`.

### Onda 1 — Backlog aprovado (SPEC-154..162)
3. SPEC-154 delta: `POST /api/cli/device` + `/api/cli/device/token` (RFC 8628), tabela `cliDeviceCodes`,
   página `/cli/device?code=`, `scripts/llmr-login.mjs`, mantendo compatíveis as rotas SPEC-050 de `CliDeviceEndpoints.cs`.
4. SPEC-155 test-mode (`LLMROUTER_TEST_MODE`), 5. SPEC-156 sinks, 6. SPEC-157 SBOM/provenance (`release.yml`),
7. SPEC-158 manifest + validador, 8. SPEC-159 provider ops (batch test), 9. SPEC-160 attempt ledger +
   `GET /api/logs/{id}/route-decision`, 10. SPEC-161 `tests/fixtures/ui-parity.json`, 11. SPEC-162 compat/upgrade.

### Onda 2 — Delta funcional v3.8.53 (novas SPEC-165..170)
12. **SPEC-165 Resilience/combo runtime** — portar `combo/{agenticStall,agenticStallFailover,attemptFailure,
    authorizedRetryWait,comboServiceTier,mediaCompatibility,preContentTransportFailure,truncatedStreamFailover}`,
    `accountFallback/antiAvalanche`, `codexAccount/capacity429`, `antigravity429Guard`, `assistantPrefill(+Error)`,
    `learnedMaxTokensCaps`, `codexQuota{Parser,Signals,WindowPolicy}`, `githubCreditLimit`, `tokenRefresh/passiveCodex`,
    `src/sse/services/{ciPayloadQuotaFailure,comboTimeoutSessionAffinity,expiryFirstMetrics,…}`; handlers
    `chatCore/{clientSessionId,streamingRoutingEvent,turnExecutionContext,streamStateBody,nonStreamingJsonDocument,
    searchProviderRejection}`, `sseParser/{truncation,responsesSnapshot}`; translator `helpers/{mediaContentParts,
    mediaIntegrity,responsesToolNameRestore}`. Grep local confirma ausência (0 arquivos) de agenticStall, truncatedStream,
    antiAvalanche, clientSessionId, queueWait.
13. **SPEC-166 Routing features (changelog)** — api-key preferred connections (#13102), p2c latency signal (#16737),
    quota-weighted pace scoring (#16572), tool disclosure BM25 (#16861), refusal operator signal/memory (#16385/#16421),
    combo stale model badge (#15923), `/v1/models` publish auto-lkgp (#16224), models-health cooldown route (#16247),
    custom model max-output override (#17140), inferred free signals (#16800), egress quota rotation (#17074).
14. **SPEC-167 Providers/executors/OAuth** — executors `muse-code`, `zcode/*`, `geminiLiveTranscribe`,
    `default/{forceStreamBody,perplexityAgentDefaults}`, `opencode*`, `commandCode/zdr`, `base/maxTokensRecovery`;
    providers novos (ainetcafe, onomeo free catalog, bedrock embeddings, elevenlabs sound/music, chatgpt-web gpt6,
    stepfun balance, nimble-lite cost); bloco `oauth` para codebuddy-cn, codebuddy-intl, cursor, devin-desktop,
    freebuff, muse-code, trae, zed-hosted + entradas novas grok-cli-oauth, zed.
15. **SPEC-168 Data + API** — equivalentes das migrations 214 (call_logs client_session_id), 217 (quota_recovery_tickets),
    219, 220 (compression combos output styles), 223 (queue_wait_ms), 225; rotas `combos/export|import`,
    `monitoring/model-health`, `pricing/unpriced-usage`, `usage/availability`, `v1/quota-recoveries/[id]`,
    `oauth/[provider]/import-export`, `settings/cli-versions`, `system/metrics`, `v0/management/{api-call,auth-files,reset-quota}`,
    `cli-tools/whycodes-settings`.
16. **SPEC-169 Teams cost centers** (T3 — modelo de dados): migrations 215/216, `/api/teams{,/[id],/[id]/members,/[id]/usage}`,
    página `/dashboard/costs/teams`, team usage limits. Hoje só existe `ApiKey.TeamId`, sem entidade Team.
17. **SPEC-170 MFA TOTP + Entra SSO** (T3 — segurança): `/api/auth/mfa/*` (5), `/api/auth/sso/config`, migration 222,
    OIDC role/group admission (#18001). Hoje existem `OidcSubject/OidcIssuer` (OIDC básico), sem MFA.

### Onda 3 — UI e i18n
18. 4 rotas: `/dashboard` (exata — conferir se cai na home), `/dashboard/api-manager/routing`, `/dashboard/costs/teams`
    (SPEC-169), `/dashboard/media-providers/{kind}`.
19. Completar as 61 chaves faltantes em `pt-BR.json` e `es.json` + um teste que compara os conjuntos de chaves com `en.json`.

### Onda 4 — Infra de testes
20. Diagnosticar o travamento do testhost (até um filtro de 3 classes passa de 250 s): checar fixtures
    `WebApplicationFactory`/SQLite compartilhados, `xunit.runner.json`, hosted services iniciando no host de teste.
    Meta: filtro por classe < 30 s e suíte inteira dentro do budget do CI.

## Files to Modify
- Docs: `.specs/README.md`, `.specs/SPEC-165..170-*.md` (novos), `docs/analysis/omniroute-gap-v3.8.53.md` (novo),
  `docs/analysis/omniroute-migration-v3.8.53.md`, `docs/analysis/pendencias.md`, `README.md`, `README.pt-BR.md`.
- Código (por SPEC): `src/LLMRouter.Server/Endpoints/*`, `src/LLMRouter.Core/{Gateway,Resilience,Routing,OAuth,Usage,Logging,Data}`,
  `src/LLMRouter.Core/Registry/providers.json`, `src/LLMRouter.Client/Pages/*`, `wwwroot/locales/{pt-BR,es}.json`,
  `.github/workflows/release.yml` (só SPEC-157), `tests/LLMRouter.Tests/*`, `scripts/llmr-login.mjs`.

## Verification
- Por PR: `dotnet build LLMRouter.slnx` com 0 erros + testes da SPEC (`--filter`) + CI verde.
- Onda 0: `git -C /tmp/omniroute rev-parse v3.8.53^{commit}` = `191dce8d…` registrado no manifest.
- Onda 3: teste de paridade de chaves i18n (en == pt-BR == es).
- Onda 2: cada serviço portado ganha um teste espelhando o `*.test.ts` correspondente do upstream.

## Risks
- Testhost travando → sem validação local de testes até a Onda 4; o CI vira a barreira.
- O upstream já está em v3.8.54 (+283 arquivos) → congelar no pin da tag e tratar a v3.8.54 numa onda separada.
- SPEC-169/170 mexem em modelo de dados e auth → aprovação explícita (T3).
- Histórico do upstream reescrito → diffs sempre por árvore.

## Open Questions
1. Teams cost centers (SPEC-169) e MFA/Entra SSO (SPEC-170): migrar? Default: migrar após a Onda 1, com aprovação T3.
2. sing-box (8 rotas + migration 221), mihomo/core-config, proxy egress/bulk, tray preferences, bifrost sidecar:
   default = out-of-scope (desktop/infra).
3. i18n além de en/pt-BR/es (upstream tem 65): default = fora do escopo.
4. Pin: tag `v3.8.53` (recomendado) ou já `release/v3.8.54`?
5. Ticket/issue de tracking.
