# SPEC-169 — Teams cost centers (upstream migrations 215/216)

Origin: OmniRoute `v3.8.53` — `src/lib/db/teams.ts`, `src/lib/db/teamUsageAnalytics.ts`, `src/lib/usage/team{CostShapes,UsageLimits}.ts`, `src/app/api/teams/**`, dashboard `costs/teams` (11 components), `docs/guides/TEAM_COST_CENTERS.md`.

## Goal

Multi-tenant cost centers: usage and budgets aggregated per team.

## Scope

- Entities `Team`, `TeamMember` (+ reuse existing `ApiKey.TeamId`).
- Routes: `/api/teams`, `/api/teams/{id}`, `/api/teams/{id}/members`, `/api/teams/{id}/usage` (upstream shapes).
- Team usage limits enforcement.
- Dashboard page `/dashboard/costs/teams` (TeamBudgetCard, TeamForm, TeamDetailPane, TeamMembership, TeamPicker, TeamReport layout).

## Acceptance criteria

- Team with 2 members and assigned keys: `GET /api/teams/{id}/usage` returns usage aggregated by member, filtered to the team (AC-10).
- Deleting a team keeps ApiKey rows consistent (orphan policy defined by upstream).
- Page matches upstream layout/i18n.

## Tests

- Route contract + aggregation tests tagged `# Covers RF-017`; UI parity via SPEC-161 fixture.