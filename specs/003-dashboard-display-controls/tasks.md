# Tasks: Dashboard Display Controls

**Feature branch**: `003-dashboard-display-controls`
**Spec**: `specs/003-dashboard-display-controls/spec.md`
**Plan**: `specs/003-dashboard-display-controls/plan.md`

## Status Legend

- `[ ]` - pending
- `[X]` - done with real evidence
- `[S]` - done with synthetic evidence only (must be disclosed per Principle IV)
- `[F]` - failed
- `[-]` - skipped (with written rationale)

The `[S*]` marker is computed, not written: any task whose dependency is
`[S]` or `[S*]` and which otherwise would be `[X]` is promoted to `[S*]` by
the evidence audit. See `readiness/task-graph.md` for the propagated view.

## Vertical-slice rule (US phases)

A task tagged `[US*]` may only be marked `[X]` when the change is
reachable from a user-facing entry point and that path was actually
exercised - an FSI session against the packed library, a smoke run of the
application, a manual walk-through with transcript, or a screenshot
captured under `readiness/`. Domain, model, or core-layer changes alone
do **not** satisfy `[X]` for a `[US*]` task, even if their unit tests
pass green. If the user-reachable surface is missing, stubbed, or not
yet wired, mark `[ ]` (work continues) or `[S]` with a disclosed reason
in the Synthetic-Evidence Inventory - never `[X]`.

This rule does not apply to Setup, Foundation, Integration, or Polish
phase tasks; those are evaluated against their own phase verification.

## Task Annotations

- **[P]** - parallel-safe (no deps inside the current phase)
- **[US1]**, **[US2]**, ... - user-story scope
- **[T1]** / **[T2]** - Tier 1 (contracted) vs Tier 2 (internal) change

Every task must have a matching entry in `tasks.deps.yml` even if its
dependency list is empty. The `speckit.graph.compute` command refuses to
proceed with dangling references.

---

## Phase 1: Setup

- [X] T001 Confirm active feature metadata points to `specs/003-dashboard-display-controls`
- [X] T002 [P] Create `specs/003-dashboard-display-controls/readiness/` for evidence transcripts, smoke output, and graph artifacts
- [X] T003 [P] Record Tier 1 public-surface impact for version display, stripe color roles, full-screen commands, and modal state in `readiness/public-surface.md`
- [X] T004 [P] Add a display-controls evidence plan covering FSI, semantic tests, render smoke checks, and package version verification

---

## Phase 2: Foundation

- [X] T005 Draft `.fsi` public surface updates for new version metadata, row stripe roles, full-screen target/model types, and dashboard command cases
- [X] T006 [P] Add failing semantic tests for command IDs, default full-screen bindings, stripe role parsing/defaults, and version fallback behavior
- [X] T007 [P] Add failing dashboard state reducer tests for opening, replacing, and closing full-screen modal targets without changing selected feature/story/task
- [X] T008 [P] Add failing rendering smoke tests for header version placement, stripe precedence helpers, and full-screen single-target rendering
- [X] T009 Exercise the draft `.fsi` surface from FSI and capture transcript to `readiness/fsi-session.txt`
- [X] T010 Record current public surface baseline before implementation in `readiness/public-surface-baseline.txt`
- [X] T011 Record unsupported-scope and safe-failure expectations for missing version metadata, missing source text, invalid stripe colors, and unavailable full-screen targets

**Checkpoint**: Foundation ready - story implementation may begin in parallel.

---

## Phase 3: User Story 1 (US1) - See Dashboard Version At A Glance

### Tests First (Principle I, Principle V)

- [X] T012 [P] [US1] Add semantic test coverage for resolving build/package version metadata and `vunknown` fallback through the public surface
- [X] T013 [P] [US1] Add rendering smoke coverage that asserts the header contains `sk-dashboard` plus a version value in wide and narrow layouts

### Implementation

- [X] T014 [US1] Implement version resolution in Core using installed assembly/package metadata with a stable fallback value
- [X] T015 [US1] Wire the resolved version into dashboard snapshot/rendering inputs without reading source checkout files at runtime
- [X] T016 [US1] Update header rendering so the dashboard name and version remain visible in widescreen and vertical layouts
- [X] T017 [US1] Add actionable diagnostics or fallback evidence for unavailable version metadata
- [X] T018 [US1] Capture version-display smoke output in `readiness/us1-version-header.txt`

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 (US2) - Scan Tables With Alternating Rows

### Tests First

- [X] T019 [P] [US2] Add semantic tests for `rowStripeOdd` and `rowStripeEven` preference parsing, defaults, invalid values, and low-contrast fallback
- [X] T020 [P] [US2] Add renderer tests proving alternating row styles apply to feature, story, task, diagnostic, and detail-style tables
- [X] T021 [P] [US2] Add renderer tests proving selected, active, warning, and error row states override stripe backgrounds

### Implementation

- [X] T022 [US2] Extend `DashboardColorRole` defaults, parsing, diagnostics, and preference contract support for row stripe roles
- [X] T023 [US2] Implement reusable table row style selection so visible non-header data rows alternate safely
- [X] T024 [US2] Apply row striping to feature, user story, task, diagnostic, and detail-like tables without changing selection or ordering behavior
- [X] T025 [US2] Update README and preference examples with stripe color roles and safe-fallback behavior
- [X] T026 [US2] Capture default, configured-color, and invalid-stripe smoke outputs in readiness artifacts

**Checkpoint**: User Story 2 is fully functional and testable independently.

---

## Phase 5: User Story 3 (US3) - Expand Current Dashboard Context Full Screen

### Tests First

- [X] T027 [P] [US3] Add semantic tests for full-screen command IDs, default bindings, and user-configured binding overrides
- [X] T028 [P] [US3] Add state reducer tests for feature/story/plan/task modal open, modal replacement, close behavior, and selection preservation
- [X] T029 [P] [US3] Add rendering tests for feature, story, plan, and task full-screen views showing parsed fields plus source text when available
- [X] T030 [P] [US3] Add missing-target tests for unavailable feature, story, plan, task, source text, and diagnostics

### Implementation

- [X] T031 [US3] Add full-screen target and modal state types to the public domain surface
- [X] T032 [US3] Extend hotkey commands, default bindings, preference parsing, scripted key handling, and footer/help text for four full-screen commands
- [X] T033 [US3] Implement app state transitions for opening each full-screen target, using selected story/task first and active fallback only when no selected item exists, replacing an open target, and closing without changing selections
- [X] T034 [US3] Load or expose associated source artifact text for selected feature, story, plan, and task targets with safe unreadable/missing handling
- [X] T035 [US3] Implement full-screen renderables for feature, story, plan, and task views with exactly one requested target type per modal
- [X] T036 [US3] Preserve existing navigation, refresh, preference reload, and quit behavior outside full-screen views
- [X] T037 [US3] Update README and quickstart examples for full-screen hotkeys and scripted smoke checks
- [X] T038 [US3] Capture feature/story/plan/task full-screen smoke transcripts in readiness artifacts

**Checkpoint**: User Story 3 is fully functional and testable independently.

---

## Phase 6: Integration & Polish

- [X] T039 Refresh surface-area baselines for changed public modules after implementation
- [X] T040 Run `dotnet test sk-Dashboard.sln` and capture output to `readiness/dotnet-test.txt`
- [X] T041 Run scripted dashboard smoke checks for default navigation plus version, stripe, full-screen flows, table-like full-screen stripe behavior, and lightweight render-cycle/performance evidence
- [X] T042 Verify package version bump/install workflow or document why package installation was not performed for this change
- [X] T043 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh --graph-only` and capture clean graph output
- [X] T044 Run the final evidence audit and resolve any `[S]`, `[S*]`, or diff-scan hits before merge readiness

---

## Synthetic-Evidence Inventory

List every `[S]` task here with its Principle IV disclosures. This section is
the source for the PR description's synthetic-evidence section.

| Task | Reason | Real-evidence path | Tracking issue |
|------|--------|---------------------|----------------|
| _(none yet)_ | | | |
