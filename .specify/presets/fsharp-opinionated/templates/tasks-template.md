# Tasks: [FEATURE_NAME]

**Feature branch**: `[FEATURE_BRANCH]`
**Spec**: `specs/[FEATURE_ID]/spec.md`
**Plan**: `specs/[FEATURE_ID]/plan.md`

## Status Legend

- `[ ]` — pending
- `[X]` — done with real evidence
- `[S]` — done with synthetic evidence only (must be disclosed per Principle IV)
- `[F]` — failed
- `[-]` — skipped (with written rationale)

The `[S*]` marker is computed, not written: any task whose dependency is
`[S]` or `[S*]` and which otherwise would be `[X]` is promoted to `[S*]` by
the evidence audit. See `readiness/task-graph.md` for the propagated view.

## Vertical-slice rule (US phases)

A task tagged `[US*]` may only be marked `[X]` when the change is
reachable from a user-facing entry point and that path was actually
exercised — an FSI session against the packed library, a smoke run of the
application, a manual walk-through with transcript, or a screenshot
captured under `readiness/`. Domain, model, or core-layer changes alone
do **not** satisfy `[X]` for a `[US*]` task, even if their unit tests
pass green. If the user-reachable surface is missing, stubbed, or not
yet wired, mark `[ ]` (work continues) or `[S]` with a disclosed reason
in the Synthetic-Evidence Inventory — never `[X]`.

This rule does not apply to Setup, Foundation, Integration, or Polish
phase tasks; those are evaluated against their own phase verification.

## Task Annotations

- **[P]** — parallel-safe (no deps inside the current phase)
- **[US1]**, **[US2]**, … — user-story scope
- **[T1]** / **[T2]** — Tier 1 (contracted) vs Tier 2 (internal) change

Every task must have a matching entry in `tasks.deps.yml` even if its
dependency list is empty. The `speckit.graph.compute` command refuses to
proceed with dangling references.

---

## Phase 1: Setup

- [ ] T001 Scaffold the feature directory and link spec + plan
- [ ] T002 [P] Add baseline install or adoption documentation for the selected profile
- [ ] T003 [P] Add readiness artifact scaffolding (`specs/[FEATURE_ID]/readiness/`)
- [ ] T004 Record feature Tier, affected layer, public-API impact, and required evidence obligations

---

## Phase 2: Foundation

- [ ] T005 Draft the public surface as `.fsi` signature(s)
- [ ] T006 [P] Add or update constitutional guidance that this feature touches
- [ ] T007 [P] Define or update operational workflows, commands, reports, or scripts
- [ ] T008 Exercise the draft `.fsi` from FSI (`scripts/prelude.fsx` or ad-hoc) and capture the session transcript to `readiness/fsi-session.txt`
- [ ] T009 Record surface-area baselines for the new / changed public modules
- [ ] T010 Record unsupported-scope handling and failure diagnostics

**Checkpoint**: Foundation ready — story implementation may begin in parallel.

---

## Phase 3: User Story 1 (US1)

### Tests First (Principle I, Principle V)

- [ ] T011 [P] [US1] Add semantic tests that load the packed library (or prelude) and exercise the US1 surface
- [ ] T012 [P] [US1] Add verification for the US1 outcome against the readiness artifact

### Implementation

- [ ] T013 [P] [US1] Add story-specific contracts, docs, or fixtures
- [ ] T014 [P] [US1] Add any required sample or schema artifacts
- [ ] T015 [US1] Implement the primary user-facing behavior for the story
- [ ] T016 [US1] Connect the story to canonical readiness artifacts or workflows
- [ ] T017 [US1] Add validation and actionable failure diagnostics
- [ ] T018 [US1] Document the story's independent validation path

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 (US2)

### Tests First

- [ ] T019 [P] [US2] Add semantic tests exercising the US2 surface through FSI
- [ ] T020 [P] [US2] Add validation for the US2 readiness outcome

### Implementation

- [ ] T021 [P] [US2] Add story-specific contracts, docs, or fixtures
- [ ] T022 [US2] Implement the primary user-facing behavior for the story

**Checkpoint**: User Story 2 is fully functional and testable independently.

---

## Phase 5: Integration & Polish

- [ ] T023 Surface-area baseline refresh (Tier 1 only)
- [ ] T024 Run the packed library through the numbered example scripts and confirm none are broken
- [ ] T025 Run `speckit.graph.compute` — confirm no cycles, no dangling refs, no `[S*]` surprises
- [ ] T026 Run `speckit.evidence.audit` — confirm verdict PASS or document every `--accept-synthetic` override

---

## Synthetic-Evidence Inventory

List every `[S]` task here with its Principle IV disclosures. This section is
the source for the PR description's synthetic-evidence section.

| Task | Reason | Real-evidence path | Tracking issue |
|------|--------|---------------------|----------------|
| _(none yet)_ | | | |
