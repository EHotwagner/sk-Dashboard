# Tasks: Speckit TUI Dashboard

**Feature branch**: `001-speckit-tui-dashboard`
**Spec**: `specs/001-speckit-tui-dashboard/spec.md`
**Plan**: `specs/001-speckit-tui-dashboard/plan.md`

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

- [X] T001 Update `Directory.Build.props` to target `net10.0` and preserve F# warning policies
- [X] T002 Scaffold `src/Core` F# library project and add it to the solution
- [X] T003 Scaffold `src/Dashboard` F# console project and add it to the solution
- [X] T004 Add Spectre.Console dependency to `src/Dashboard/Dashboard.fsproj`
- [X] T005 Scaffold `tests/Core.Tests` and `tests/Dashboard.Tests` Expecto projects and remove or migrate existing `src/Lib` and `tests/Lib.Tests` placeholder projects from the solution
- [X] T006 [P] Create `specs/001-speckit-tui-dashboard/readiness/` for transcripts, screenshots, and graph evidence
- [X] T007 [P] Update `README.md` with build, run, and test commands for the dashboard app
- [X] T008 Record feature tier, affected public surfaces, and evidence obligations in `readiness/evidence-plan.md`

**Checkpoint**: Setup complete — solution shape and evidence paths are ready.

---

## Phase 2: Foundation

- [X] T009 Draft public `.fsi` signatures for domain models, diagnostics, and dashboard snapshots in `src/Core`
- [X] T010 Draft public `.fsi` signatures for Speckit artifact discovery and parsing
- [X] T011 Draft public `.fsi` signatures for Git feature discovery and checkout operations
- [X] T012 Draft public `.fsi` signatures for task graph construction and validation
- [X] T013 Draft public `.fsi` signatures for hotkey command maps and configuration validation
- [X] T014 Implement shared domain records and diagnostic severity types in `src/Core/Domain.fs`
- [X] T015 Implement safe process execution wrapper for Git CLI calls in `src/Core`
- [X] T016 Implement repository path and user config path resolution helpers in `src/Core`
- [X] T017 Add baseline parser and state reducer test fixtures for empty, partial, complete, and malformed Speckit projects
- [X] T018 Add FSI/prelude session coverage for the drafted core public surface and save transcript to `readiness/fsi-session.txt`
- [X] T019 Record public surface baseline for `src/Core` signatures in `readiness/public-surface.txt`
- [X] T020 Document unsupported terminal and repository-state diagnostics in `readiness/unsupported-scope.md`

**Checkpoint**: Foundation ready — story implementation may begin in parallel.

---

## Phase 3: User Story 1 (US1) - Open a Useful Dashboard from Any Project State

### Tests First

- [X] T021 [P] [US1] Add Core.Tests coverage for no `specs/` directory, empty `specs/`, and missing feature artifacts
- [X] T022 [P] [US1] Add Dashboard.Tests coverage for empty-state snapshot rendering without throwing
- [X] T023 [P] [US1] Add Core.Tests coverage for partial artifacts producing present/missing/unreadable states
- [X] T024 [P] [US1] Add Dashboard.Tests coverage for manual and automatic refresh events coalescing into a new snapshot

### Implementation

- [X] T025 [US1] Implement Speckit artifact discovery for absent, partial, and complete feature directories
- [X] T026 [US1] Implement feature status summarization for spec, plan, tasks, and checklist artifact states, including missing, present, unreadable, malformed, and locally complete states
- [X] T027 [US1] Implement dashboard startup state loading that returns an empty actionable state instead of exiting
- [X] T028 [US1] Implement file watcher plus polling fallback refresh orchestration
- [X] T029 [US1] Render empty, partial, and diagnostic states with visible refresh, quit, branch-selection, and Speckit-artifact guidance actions when applicable
- [X] T030 [US1] Capture a manual smoke transcript for an empty project and partial artifact project in `readiness/us1-empty-state.txt`

**Checkpoint**: US1 is independently testable from an empty or partial project.

---

## Phase 4: User Story 2 (US2) - Review the Latest Feature Status Automatically

### Tests First

- [X] T031 [P] [US2] Add Core.Tests coverage for numeric, timestamp, and fallback feature branch ordering
- [X] T032 [P] [US2] Add Core.Tests coverage for deterministic tie-breaking when feature branches share an order key
- [X] T033 [P] [US2] Add integration tests with a temporary Git repository containing at least 10 feature branches
- [X] T034 [P] [US2] Add integration test coverage for checkout failure caused by local project state

### Implementation

- [X] T035 [US2] Implement Git feature branch listing and Speckit feature-name parsing
- [X] T036 [US2] Implement latest-feature selection using parsed ordering and deterministic tie-breaking
- [X] T037 [US2] Implement startup auto-checkout of the latest feature branch with visible error diagnostics
- [X] T038 [US2] Wire selected feature artifact loading after successful or failed checkout
- [X] T039 [US2] Capture readiness evidence for startup selection under 10 branches and checkout failure handling

**Checkpoint**: US2 is independently testable with multiple feature branches.

---

## Phase 5: User Story 3 (US3) - Navigate Features and User Stories by Keyboard

### Tests First

- [X] T040 [P] [US3] Add Core.Tests coverage for user story extraction from feature specifications
- [X] T041 [P] [US3] Add Dashboard.Tests coverage for feature navigation commands updating selected feature state
- [X] T042 [P] [US3] Add Dashboard.Tests coverage for story navigation commands updating selected story state

### Implementation

- [X] T043 [US3] Implement user story parsing with priority, acceptance scenarios, and source locations
- [X] T044 [US3] Implement default keyboard command routing for feature, story, pane, refresh, checkout, details, and quit commands
- [X] T045 [US3] Render feature navigation and left-side story pane with selected item highlighting
- [X] T046 [US3] Implement manual checkout of older feature branches and visible blocked-action diagnostics
- [X] T047 [US3] Capture keyboard-only manual walkthrough for feature and story navigation in `readiness/us3-keyboard-navigation.txt`

**Checkpoint**: US3 is independently testable using only keyboard input.

---

## Phase 6: User Story 4 (US4) - Inspect Plan and Task Graph Details

### Tests First

- [X] T048 [P] [US4] Add Core.Tests coverage for plan extraction and missing-plan diagnostics
- [X] T049 [P] [US4] Add Core.Tests coverage for task parsing with raw status preservation
- [X] T050 [P] [US4] Add Core.Tests coverage for selected-story dependency chains including cross-story dependencies
- [X] T051 [P] [US4] Add Core.Tests coverage for missing task references, duplicate task IDs, and cycle diagnostics
- [X] T052 [P] [US4] Add Dashboard.Tests coverage for task node selection and detail pane state

### Implementation

- [X] T053 [US4] Implement plan loading and separate plan-pane model
- [X] T054 [US4] Implement task artifact parsing with raw status, dependencies, story relationship, and source location
- [X] T055 [US4] Implement DAG construction for selected-story tasks plus dependency chains
- [X] T056 [US4] Render task graph, raw task statuses, malformed-relationship diagnostics, and keyboard node selection
- [X] T057 [US4] Render task detail pane containing title, description, raw status, dependencies, story relationship, and source location
- [X] T058 [US4] Capture readiness evidence for plan pane, task graph, cross-story dependency, and malformed graph scenarios

**Checkpoint**: US4 is independently testable with a feature containing plan and task artifacts.

---

## Phase 7: User Story 5 (US5) - Customize Keyboard Controls

### Tests First

- [X] T059 [P] [US5] Add Core.Tests coverage for default command bindings covering every primary command
- [X] T060 [P] [US5] Add Core.Tests coverage for global config loading, unsupported key sequences, and conflict diagnostics
- [X] T061 [P] [US5] Add Dashboard.Tests coverage for hotkey reload applying valid overrides

### Implementation

- [X] T062 [US5] Implement global hotkey config path resolution and JSON loading
- [X] T063 [US5] Implement hotkey validation, conflict reporting, and fallback to defaults
- [X] T064 [US5] Wire validated custom bindings into dashboard command routing and reload command
- [X] T065 [US5] Capture readiness evidence for default bindings, custom override, and conflict diagnostics

**Checkpoint**: US5 is independently testable with global user hotkey configuration.

---

## Phase 8: Integration & Polish

- [X] T066 Run full `dotnet test` and fix failures across core, dashboard, and integration suites
- [X] T067 Run `dotnet run --project src/Dashboard -- .` smoke test from the repository root and capture transcript
- [X] T068 Verify operation in Emacs vterm or capture an equivalent terminal transcript documenting compatibility
- [X] T069 Verify compact terminal layout keeps all panes reachable through focus changes or scrolling
- [X] T070 Verify performance targets for empty project startup, 10-branch latest selection, and 2-second refresh behavior
- [X] T071 Refresh README and quickstart documentation with final command names and configuration path details
- [X] T072 Refresh public surface baseline after implementation changes
- [X] T073 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh --graph-only` and confirm no cycles or dangling refs
- [X] T074 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh` and record PASS or any accepted synthetic evidence rationale

---

## Synthetic-Evidence Inventory

List every `[S]` task here with its Principle IV disclosures. This section is
the source for the PR description's synthetic-evidence section.

| Task | Reason | Real-evidence path | Tracking issue |
|------|--------|---------------------|----------------|
| _(none yet)_ | | | |
