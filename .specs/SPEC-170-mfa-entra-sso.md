# SPEC-170 — MFA TOTP + Entra SSO config

Origin: OmniRoute `v3.8.53` — `src/lib/auth/totp.ts`, `src/lib/db/dashboardMfa.ts`, `src/server/auth/{dashboardMfa,mfaRouteSupport}.ts`, `src/app/api/auth/mfa/**` (5 routes), `src/app/api/auth/sso/config`, migration 222 `entra_sso_identities`, #18001 OIDC role/group admission, `settings/components/MfaSection.tsx`.

## Goal

MFA (TOTP) for dashboard accounts and an SSO config surface; OIDC admission by role/group.

## Scope

- TOTP RFC 6238: setup (secret+QR), enable, verify, disable, status; recovery codes; secret encrypted at rest.
- Re-verification for sensitive settings changes (upstream semantics).
- `/api/auth/sso/config` (GET/PUT) — config only, no IdP implementation; secrets never echoed back.
- Entra identity storage (migration 222 equivalent) + role/group admission rules (#18001).
- Profile UI MFA section.

## Acceptance criteria

- Active MFA + login without valid TOTP → denied (AC-11).
- Replayed TOTP code rejected.
- Login disabled + protected ops still require the flows above (consistency with SPEC-165 RF-013b).

## Tests

- TOTP vector tests (RFC 6238), replay, lockout, route auth matrix tagged `# Covers RF-018`.