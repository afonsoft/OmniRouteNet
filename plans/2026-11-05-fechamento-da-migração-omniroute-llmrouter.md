# Plano: Fechamento da migração OmniRoute → LLMRouter (rebaseline v3.8.53 tip)

## Summary

Análise feita em 2026-11-05 contra dois alvos:

1. **Backlog interno aprovado e não executado** — SPEC-154..162 (onda "Antirion" + contract de release).
2. **Drift do upstream** — o pin atual (`412319f0`, 2026-10-26) está **5.812 arquivos atrás** do tip de
   `release/v3.8.53` (`785a3576`). O upstream evoluiu: 31 rotas de API novas, ~25 páginas de dashboard
   novas/remodeladas, 29 fluxos OAuth de CLI-agents, multi-tenant **Teams** (18 arquivos), **MFA/SSO**
   (26 arquivos), serviço **sing-box** (15 arquivos), 18 serviços novos em `src/sse/services/` e catálogo
   de 65 locales de i18n (LLMRouter tem 3).

Conclusão: a migração do núcleo (gateway 4 formatos, combos + 20 estratégias, resilience, registry com
3.262 providers, ~818 endpoints, 100 páginas) está **madura e funcional**. O que falta é:
**(a) executar 9 SPECs aprovadas ainda sem código; (b) rebaseline do contract de upstream para o tip
785a3576; (c) absorver o delta funcional novo (Teams, MFA/SSO, sing-box, OAuth CLI-agents, 18 serviços
sse, rotas novas); (d) fechar paridade de dashboard (~25 páginas) e (e) enxugar o backlog morto.**

Estado verificado: build OK (0 erros, 80 warnings), suite de testes excede 300 s no sandbox (usar CI).

## Evidência principal (comandos para reproduzir)

- `git -C /tmp/omniroute diff --name-status ac5b16cf 7aa0b161 | wc -l` → 5.812 arquivos.
- Novas rotas API: `git diff --name-status ac5b16cf..7aa0b161 -- src/app/api | grep "^A" | grep route.ts` → 31.
- Páginas dashboard upstream: 124; LLMRouter: 100 arquivos .razor; páginas sem equivalente: ver lista em T7.
- SPEC-154/155/156/157/158/159/160/161/162: sem código correspondente (grep em `src/`, `tests/`, `.github/`).
- Build local: `dotnet build LLMRouter.slnx` → 0 erros.

## Implementation Steps

### Onda 1 — Fechar o backlog aprovado (SPEC-154..162) — prioridade máxima

1. **T1 — SPEC-154 (device-auth CLI)**: `POST /api/cli/device` + `/api/cli/device/token`, página
   `/cli/device?code=`, tabela `cliDeviceCodes`, `llmr login` em `scripts/`. Arquivos:
   `src/LLMRouter.Server/Endpoints/` (novo `CliDeviceEndpoints.cs`), `src/LLMRouter.Core/Data/`
   (migration), `src/LLMRouter.Client/Pages/CliDevice.razor` (novo), `scripts/llmr-login.mjs`.
2. **T2 — SPEC-155 (test-mode API)**: endpoints `/api/test/{reset,seed,clock,fail}` gated por
   `LLMROUTER_TEST_MODE=1` (404 fora dele); atualizar `e2e/smoke.mjs` para seed.
   Arquivo: `src/LLMRouter.Server/Endpoints/TestModeEndpoints.cs` (novo).
3. **T3 — SPEC-156 (log/audit sinks)**: `settings.sinks[]`, BackgroundService + bounded channel,
   NDJSON batch 5s/50 itens, retry 3×, `POST /api/sinks/:id/test`. Arquivos:
   `src/LLMRouter.Core/Logging/SinkWriter.cs` (novo), endpoint novo, UI em `Settings.razor`.
4. **T4 — SPEC-157 (SBOM + provenance)**: `.github/workflows/release.yml` + Syft/attest-build-provenance
   (sem tocar `ci.yml`); artefatos anexados ao GitHub Release.
5. **T5 — SPEC-158 (release contract)**: `docs/release-manifest.json` (repo/tag/commit/date/scope),
   validador determinístico (script + teste), checklist de revalidação; READMEs linkam o manifest.
6. **T6 — SPEC-159 (provider ops parity)**: agrupamento por auth/categoria, batch test com concorrência
   limitada, health/last-test/cooldown/quota/cred-expiração na UI de Providers. Arquivos:
   `src/LLMRouter.Core/ProviderOps/`, `src/LLMRouter.Client/Pages/Providers.razor`.
7. **T7 — SPEC-160 (routing decision observability)**: attempt ledger persistido (candidate order,
   strategy, skips, status, latência, cooldown, predicate, alvo escolhido), endpoint
   `/api/logs/{id}/route-decision`, coluna/aba nos Logs. Arquivos: `src/LLMRouter.Core/Usage/`,
   `src/LLMRouter.Core/Routing/`, `Logs.razor`.
8. **T8 — SPEC-161 (fidelity fixtures)**: manifest de UI parity (rota upstream ↔ página .razor, i18n key
   coverage, temas, screenshots CI) em `tests/fixtures/ui-parity.json` + teste que valida cobertura.
9. **T9 — SPEC-162 (compat/upgrade)**: fixture SQLite do OmniRoute + teste de import round-trip
   (providers, keys, combos, usageHistory), teste de upgrade de schema do LLMRouter.
10. **T10 — SPEC-145 (reconciliação)**: marcar arquivadas as SPECs já cobertas por código (verificar
    SPEC-055..153 que têm código mas seguem listadas), atualizar `.specs/README.md`.

### Onda 2 — Rebaseline do contract de upstream (SPEC-158 executado como porta de entrada)

11. **T11 — Atualizar pin**: `docs/release-manifest.json` → commit `785a357631c10591c158cf02700d6ec84c11be57`
    (tip de `release/v3.8.53`), com escopo do diff de 5.812 arquivos registrado como baseline de migração.
    Regenerar contagens: providers (3.262 no registry atual), rotas API (788 upstream vs ~818 endpoints
    locais), páginas (124 vs 100).
12. **T12 — Executar o checklist de revalidação** da SPEC-158 sobre o novo pin, classificando cada área
    como `ported` / `partial` / `intentionally out of scope` / `missing` — resultado vira
    `docs/analysis/omniroute-gap-785a3576.md` (novo documento de referência).

### Onda 3 — Delta funcional novo do upstream

13. **T13 — Teams multi-tenant** (18 arquivos upstream; **[A DEFINIR]**: aceitar escopo — ver Open Questions):
    entidades Team/TeamMember, rotas `/api/teams{,/[id],/[id]/members,/[id]/usage}`, página
    `Costs/Teams` com usage por team, permissões por team.
14. **T14 — MFA + SSO config** (26 arquivos): `/api/auth/mfa/{setup,enable,verify,disable,status}`,
    TOTP na entidade User, página Profile; `/api/auth/sso/config` (OIDC/SAML config, sem IdP real).
15. **T15 — OAuth de CLI-agents** (29 providers em `src/lib/oauth/providers/`): antigravity,
    devin-desktop, muse-code, kimi-coding, trae, qoder, kilocode, freebuff, codebuddy-*, grok-cli*,
    zed/zed-hosted, cline, kiro, ghe-copilot, gitlab-duo etc. — hoje `OAuthService.cs` tem só 3 cases;
    incluir device-flow/PKCE genérico parametrizado pelo registry (`oauth` block já existe em 18 entradas
    do `providers.json`), mapeando `clientIdEnv`/`clientSecretEnv` (SPEC-002 follow-up).
16. **T16 — sing-box service** (15 arquivos; **[A DEFINIR]** — desktop-only no upstream; proposta:
    marcar `intentionally out of scope` para o target docker; se aceito, implementar só status/stop).
17. **T17 — 18 serviços novos de `src/sse/services/`**: authTerminalStatus, comboTimeoutSessionAffinity,
    connectionModelInventory, expiryFirstAccountSelection/Metrics, githubCreditLimitPolicy,
    githubResponsesFallback, modelScopedQuotaFailure, museSessionOwnership, quotaLimitPolicy,
    quotaPreflightPolicy, requestShape, sameAccountTransportRetry, sessionAffinityPin, tokenRefresh,
    noAuthProviderSettings, model, auth — portar semântica 1:1 para `src/LLMRouter.Core/Gateway|Resilience`.
18. **T18 — Rotas de API novas (31)**: mfa×5, sso/config, whycodes-settings, combos export/import,
    monitoring/model-health, oauth import-export, pricing/unpriced-usage, sing-box×8, teams×4,
    usage/availability, v0/management {api-call, auth-files, reset-quota}, quota-recoveries/[id],
    systemone. Mapear cada uma para endpoint ASP.NET correspondente (algumas caem em T13/T14/T16).

### Onda 4 — Paridade de dashboard

19. **T19 — Páginas faltantes** (do gap-scan, ~25 rotas): acp-agents, agent-skills, audit/a2a, audit/mcp,
    cache/media, chaos, cli-code/[id], cloud-agents, combos/remove, costs/teams (T13), discovery,
    free-provider-rankings, free-tiers, leaderboard, log-export (existe: LogExport.razor), memory,
    omni-skills, orchestration, plugins/[name]/config, providers/new, providers/services, radar/{combos,
    intel, offers, setup}, resilience/{connections,cooldowns}, search-tools, settings/{access-tokens,
    advanced, ai, appearance, cache, feature-flags, general, modality-bridge, pricing, resilience,
    routing, security, sidebar}, system/{1proxy,mitm-proxy,proxy}, tools/agent-bridge, webhooks.
    Reavaliar com SPEC-161 manifest: várias já têm equivalente parcial (LogExport, Context/*,
    Analytics/*, Gamification/*, SessionPools, Radar, PxPipe, TrafficInspector) — o gap real esperado
    fica em ~15 páginas novas + sub-rotas de settings.
20. **T20 — i18n**: manter en/pt-BR/es 100% (padrão atual) e documentar os outros 62 locales como
    `intentionally out of scope` (ver Open Questions).

### Onda 5 — Qualidade e distribuição

21. **T21 — Testes**: cada onda com testes xUnit cobrindo os RFs; meta: suite < 5 min (paralelização
    `xunit.parallelizeTestCollections`), CI verde.
22. **T22 — Docs**: atualizar `docs/analysis/pendencias.md`, `feature-matrix.md` e README (release notes)
    a cada onda concluída.

## Files to Modify

- Novos: `src/LLMRouter.Server/Endpoints/{CliDevice,TestMode,Teams,Mfa,Sso,SingboxStatus}Endpoints.cs`,
  `src/LLMRouter.Core/Logging/SinkWriter.cs`, `src/LLMRouter.Core/OAuth/CliAgentOAuthProviders.cs`,
  `src/LLMRouter.Core/{Teams,Mfa}/` , `tests/fixtures/ui-parity.json`,
  `tests/LLMRouter.Tests/*Compat*,*Sink*,*Device*,*Team*,*Mfa*,*Ledger*.cs`,
  `docs/release-manifest.json`, `docs/analysis/omniroute-gap-785a3576.md`, `scripts/llmr-login.mjs`.
- Modificados: `src/LLMRouter.Core/Data/Entities.cs` + migrations, `src/LLMRouter.Core/OAuth/OAuthService.cs`,
  `src/LLMRouter.Core/Usage/*`, `src/LLMRouter.Core/Routing/*` (attempt ledger),
  `src/LLMRouter.Client/Pages/{Providers,Logs,Settings,Profile,Costs}.razor` + ~15 páginas novas,
  `src/LLMRouter.Client/wwwroot/locales/*.json`, `.github/workflows/release.yml`,
  `.specs/README.md`, `docs/analysis/pendencias.md`, `README.md`/`README.pt-BR.md`.

## Verification

- Onda 1: `dotnet test` — novos testes de sink/device/test-mode/manifest/ledger verde; `dotnet build` 0 erros.
- Onda 2: script do manifest resolve commit 785a3576 e diff-stat bate com o registrado.
- Onda 3: para cada feature (teams/mfa/sso/oauth/sse-services) um teste de contrato endpoint + e2e smoke.
- Onda 4: SPEC-161 manifest valida cobertura de rotas/páginas ≥ alvo acordado (100% core, gaps documentados).
- Contínuo: CI (build+test+docker smoke) verde em cada PR; um branch/PR por SPEC
  (`devin/spec-NNN-slug`), conforme regra do repo.

## Risks

- **Escopo do drift é grande** (5.812 arquivos upstream): mitigado por rebaseline documentado (T11/T12)
  e porclassificação `ported/partial/out-of-scope/missing` explícita.
- **Teams/MFA** tocam modelo de dados e auth (T3 no regime de risco): exigem aprovação antes de implementar.
- Suite de testes longa (>300 s local): paralelizar e manter CI como barreira.
- OAuth de 29 providers depende de clientIds/secrets externos: implementar fluxos + env vars, sem embutir
  segredo (push protection).
- Desktop-only (sing-box, electron, MITM TLS): manter `intentionally out of scope` documentado para
  evitar paridade falsa.

## Open Questions (bloqueiam Onda 3/4 parcialmente)

1. **Teams multi-tenant**: migrar (fiel ao upstream) ou registrar como out-of-scope?
2. **i18n 65 locales**: manter 3 idiomas completos ou gerar os 65?
3. **SPEC-157..162 vs drift**: confirmada prioridade da Onda 1 (backlog aprovado) antes do drift novo?
