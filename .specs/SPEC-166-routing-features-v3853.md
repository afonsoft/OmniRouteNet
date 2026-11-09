# SPEC-166 — Routing features v3.8.53 (changelog wave)

Origin: OmniRoute `v3.8.53` `changelog.d/features` vs pin `412319f0`.

## Goal

Implement the eleven routing-behavior features shipped in the pin→tag delta.

## Scope

- #13102 api-key preferred connections.
- #16737 p2c (power-of-two) latency signal.
- #16572 quota-weighted pace scoring.
- #16861 tool disclosure BM25.
- #16385/#16421 refusal operator signal + refusal memory address key.
- #15923 combo stale model badge.
- #16224 `/v1/models` publish auto-lkgp.
- #16247 models-health cooldown route.
- #17140 custom model max-output-tokens override.
- #16800 inferred free signals.
- #17074 egress quota rotation (all providers).

## Acceptance criteria

- Each numbered feature matches upstream behavior (source: the feature's `changelog.d/features/*.md` + implementation at `191dce8d`).
- Routing changes are visible in existing strategy surfaces; no new public gateway contract beyond what upstream shipped.

## Tests

- One test per feature, tagged `# Covers RF-014`, derived from upstream behavior/tests.