# SPEC-167 — Providers, executors & OAuth v3.8.53

Origin: OmniRoute `v3.8.53` delta vs pin `412319f0` — `open-sse/executors/*`, new providers, `src/lib/oauth/providers/*` (29 files upstream; local OAuth is registry-driven generic device/pkce/poll).

## Goal

Port the new executors/handlers, the provider catalog additions, and complete the registry `oauth` blocks.

## Scope

- Executors: muse-code, zcode/{gateway,nativeProtocol,profileAuth}, geminiLiveTranscribe, default/{forceStreamBody,museCode,perplexityAgentDefaults}, opencode{FreeTierEgressProbe,MuseSparkEffort,ResponsesFields}, commandCode/zdr, base/maxTokensRecovery, codex/ssePeekFailure, gemini-web/browserErrors.
- Handlers: grokMedia, systemOne, search/kimiSearch.
- Providers: #13862 ainetcafe, #16325 onomeo free catalog, #16869 bedrock embeddings, #17287 elevenlabs sound/music, #17840 chatgpt-web gpt6 models, #16348 stepfun usage balance, #17965 nimble-lite cost, #17345 opencode-zen free models refresh.
- OAuth: add `oauth` block for registry ids codebuddy-cn, codebuddy-intl, cursor, devin-desktop, freebuff, muse-code, trae, zed-hosted; add new registry entries grok-cli-oauth, zed. All flows via env vars (`clientIdEnv`/`clientSecretEnv`) — no secrets in repo.

## Acceptance criteria

- OAuth connect works for each of the 10 targets when its env vars are set; missing var → clear error naming the var.
- New providers appear in the registry with models/pricing/capabilities matching upstream.
- Executors preserve upstream request/response shaping.

## Tests

- Registry invariants (schema, no embedded secrets, oauth env-var resolution) tagged `# Covers RF-015`.
- Executor behavior tests mirroring upstream `*.test.ts` where they exist.