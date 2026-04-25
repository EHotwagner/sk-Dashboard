# Tasks: Configurable Dashboard UI

**Feature branch**: `002-configurable-dashboard-ui`
**Spec**: `specs/002-configurable-dashboard-ui/spec.md`
**Plan**: `specs/002-configurable-dashboard-ui/plan.md`

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

- [X] T001 Create `specs/002-configurable-dashboard-ui/readiness/` for FSI transcripts, layout smoke captures, and validation evidence
- [X] T002 [P] Record Tier 1 public-surface impact and evidence obligations in `specs/002-configurable-dashboard-ui/readiness/evidence-plan.md`
- [X] T003 [P] Add preference contract examples from `contracts/dashboard-ui-preferences.md` to readiness fixtures
- [X] T004 [P] Review existing hotkey config docs and identify README/quickstart sections that must mention UI preferences

---

## Phase 2: Foundation

- [X] T005 Draft updated public `.fsi` signatures for dashboard preferences, UI preferences, color roles, layout modes, and validation diagnostics in `src/Core`
- [X] T006 [P] Add Core.Tests coverage for parsing a combined dashboard preferences file containing both `bindings` and `ui`
- [X] T007 [P] Add Core.Tests coverage for default UI preferences when `ui` is absent
- [X] T008 [P] Add Core.Tests coverage for named color values and hex RGB color values
- [X] T009 [P] Add Core.Tests coverage for invalid colors, unknown color roles, unsupported layout modes, and partial fallback diagnostics
- [X] T010 [P] Add Core.Tests coverage for low-contrast foreground/background pairs falling back to defaults
- [X] T011 [P] Add Dashboard.Tests coverage for automatic layout selection below 120 columns and at 120+ columns
- [X] T012 Exercise the drafted preference-loading public surface from FSI and save transcript to `readiness/fsi-session.txt`
- [X] T013 Implement core preference domain types and defaults for UI settings in `src/Core`
- [X] T014 Implement dashboard preference loading that preserves existing hotkey behavior while adding optional UI settings
- [X] T015 Implement color value parsing and validation for named terminal colors and hex RGB colors
- [X] T016 Implement low-contrast detection and fallback diagnostics for foreground/background color pairs
- [X] T017 Implement layout mode parsing and automatic layout decision rules
- [X] T018 Refresh public surface baseline for changed Core signatures in `readiness/public-surface.txt`
- [X] T055 Validate changed public surface baseline with an automated test covering the updated Core `.fsi` signatures

**Checkpoint**: Foundation ready — story implementation may begin in parallel.

---

## Phase 3: User Story 1 (US1) - Customize Dashboard Colors

### Tests First

- [X] T019 [P] [US1] Add Dashboard.Tests rendering coverage for configured colors appearing on selected rows, last activity, progress, diagnostics, muted text, and panel accents
- [X] T020 [P] [US1] Add Dashboard.Tests coverage that built-in colors are used when no custom colors are configured
- [X] T021 [P] [US1] Add dashboard smoke scenario using a real preference file with named colors and hex RGB colors

### Implementation

- [X] T022 [US1] Route validated UI color preferences from startup and reload paths into dashboard render state
- [X] T023 [US1] Replace hardcoded renderer color literals with configurable color role lookups in `src/Dashboard/Render.fs`
- [X] T024 [US1] Apply color role preferences consistently to feature rows, story rows, task rows, progress bars, diagnostics, muted text, and panel accents
- [X] T025 [US1] Surface invalid color and low-contrast fallback diagnostics in the dashboard diagnostics pane
- [X] T026 [US1] Document color roles, named color values, hex RGB values, and fallback behavior in README and quickstart
- [X] T027 [US1] Capture manual color-configuration evidence in `readiness/us1-configurable-colors.txt`

**Checkpoint**: US1 is independently testable with custom colors and safe defaults.

---

## Phase 4: User Story 2 (US2) - Choose Widescreen or Vertical Layout

### Tests First

- [X] T028 [P] [US2] Add Dashboard.Tests coverage for explicit `widescreen`, explicit `vertical`, and `auto` layout modes
- [X] T029 [P] [US2] Add Dashboard.Tests coverage that `auto` uses vertical below 120 columns and widescreen at 120+ columns
- [X] T030 [P] [US2] Add rendering smoke coverage that vertical layout keeps primary section headers readable in a narrow terminal
- [X] T056 [P] [US2] Add Dashboard.Tests coverage that feature, story, and task keyboard navigation works in widescreen, vertical, and auto layout modes

### Implementation

- [X] T031 [US2] Implement layout selection plumbing from validated UI preferences into dashboard rendering
- [X] T032 [US2] Split the current renderer into reusable section renderables that can be composed as widescreen or vertical layouts
- [X] T033 [US2] Implement widescreen layout preserving side-by-side navigation and detail context
- [X] T034 [US2] Implement vertical layout stacking primary sections in readable order
- [X] T035 [US2] Implement automatic layout selection based on the 120-column threshold
- [X] T036 [US2] Preserve selected feature, story, and task state across layout changes and preference reloads
- [X] T057 [US2] Capture smoke evidence for keyboard navigation in widescreen, vertical, and auto layout modes
- [X] T037 [US2] Document layout modes and automatic threshold in README and quickstart
- [X] T038 [US2] Capture wide and narrow layout evidence in `readiness/us2-layout-options.txt`

**Checkpoint**: US2 is independently testable with explicit and automatic layouts.

---

## Phase 5: User Story 3 (US3) - Recover from Invalid UI Preferences

### Tests First

- [X] T039 [P] [US3] Add Core.Tests coverage for unreadable, empty, malformed, and partially valid dashboard preference files
- [X] T040 [P] [US3] Add Dashboard.Tests coverage that valid preferences still apply when sibling UI preferences are invalid
- [X] T041 [P] [US3] Add dashboard smoke scenario for invalid layout and invalid color values producing visible diagnostics

### Implementation

- [X] T042 [US3] Ensure preference loading accumulates actionable diagnostics for every invalid UI setting
- [X] T043 [US3] Ensure invalid UI preferences never prevent dashboard startup or live reload
- [X] T044 [US3] Ensure valid hotkeys and valid UI preferences continue to apply when other preference values are invalid
- [X] T045 [US3] Render preference diagnostics with source context where available
- [X] T046 [US3] Document invalid preference recovery behavior and examples in README and quickstart
- [X] T047 [US3] Capture invalid-preference recovery evidence in `readiness/us3-invalid-preferences.txt`

**Checkpoint**: US3 is independently testable with invalid preference files.

---

## Phase 6: Integration & Polish

- [X] T048 Run full `dotnet test` and fix failures across Core.Tests and Dashboard.Tests
- [X] T049 Run FSI/prelude evidence for the final public preference-loading surface and refresh `readiness/fsi-session.txt`
- [X] T050 Run dashboard smoke checks for default, configured-color, widescreen, vertical, auto, and invalid-preference scenarios
- [X] T051 Update `README.md` and `specs/002-configurable-dashboard-ui/quickstart.md` with final preference examples and commands
- [X] T052 Bump package version in `Directory.Build.props`, pack to `~/.local/share/nuget-local`, and update the global `sk-dashboard` tool
- [X] T053 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh --graph-only` and record PASS in `readiness/final-graph-audit.txt`
- [X] T054 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh` and record PASS or accepted-synthetic rationale in `readiness/final-evidence-audit.txt`

---

## Synthetic-Evidence Inventory

List every `[S]` task here with its Principle IV disclosures. This section is
the source for the PR description's synthetic-evidence section.

| Task | Reason | Real-evidence path | Tracking issue |
|------|--------|---------------------|----------------|
| _(none yet)_ | | | |
