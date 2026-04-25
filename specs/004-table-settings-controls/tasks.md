# Tasks: Table Settings Controls

**Feature branch**: `004-table-settings-controls`
**Spec**: `specs/004-table-settings-controls/spec.md`
**Plan**: `specs/004-table-settings-controls/plan.md`

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

- [X] T001 Confirm the feature branch, spec, plan, contracts, data model, and quickstart are present for `004-table-settings-controls`
- [X] T002 [P] Create readiness scaffolding under `specs/004-table-settings-controls/readiness/` for FSI transcripts, smoke logs, config fixtures, and rendered-output captures
- [X] T003 [P] Record the Tier 1 contract surfaces, affected modules, and required real-evidence paths in `specs/004-table-settings-controls/readiness/evidence-plan.md`
- [X] T004 Map existing dashboard tables, detail surfaces, hotkeys, and config-loading entry points to the feature requirements

---

## Phase 2: Foundation

- [X] T005 Draft public Core `.fsi` signatures for table viewport state, detail viewport state, display settings, color roles, config file state, settings edit sessions, and new command identifiers
- [X] T006 [P] Add semantic test scaffolding in `tests/Core.Tests` for settings parsing, validation defaults, hotkey command registration, viewport reducers, and conflict metadata
- [X] T007 [P] Add dashboard reducer and rendering smoke test scaffolding in `tests/Dashboard.Tests` for scrollable tables, detail scrolling, settings surfaces, and live reload
- [X] T008 Exercise the draft public signatures from FSI and capture the transcript in `specs/004-table-settings-controls/readiness/fsi-session.txt`
- [X] T009 Record public surface baselines for changed `.fsi` modules in `specs/004-table-settings-controls/readiness/surface-baseline.txt`
- [X] T010 Define config diagnostics and safe fallback messages for invalid borders, colors, hotkeys, unreadable files, stale saves, and deferred reloads
- [X] T011 Define shared JSON config fixtures covering defaults, unknown future fields, invalid known fields, border styles, detail color roles, and live reload settings
- [X] T012 Run the foundation test scaffolds once and capture the expected failing or pending evidence in `specs/004-table-settings-controls/readiness/foundation-test-baseline.txt`

**Checkpoint**: Foundation ready — story implementation may begin in parallel.

---

## Phase 3: User Story 1 (US1)

### Tests First (Principle I, Principle V)

- [X] T013 [P] [US1] Add Core semantic tests for table viewport clamping, selection-anchored vertical scrolling, horizontal offsets, sticky columns, empty rows, deleted selections, and terminal resize
- [X] T014 [P] [US1] Add Dashboard smoke fixtures with oversized feature, story, task, diagnostic, and detail-oriented table data including 500-row and wide-cell cases
- [X] T015 [P] [US1] Add Dashboard smoke tests for full detail pages with at least 2,000 lines, vertical scrolling, horizontal scrolling, and close/reopen context restoration

### Implementation

- [X] T016 [US1] Implement typed table and detail viewport reducers in `src/Core/Domain.fs` with `.fsi` exposure and safe clamp behavior
- [X] T017 [US1] Wire vertical selection-anchored scrolling into all dashboard table navigation paths in `src/Dashboard/App.fs` and `src/Dashboard/Input.fs`
- [X] T018 [US1] Wire explicit horizontal table and detail scroll commands through `src/Core/Hotkeys.fs`, `src/Dashboard/Input.fs`, and dashboard state
- [X] T019 [US1] Update `src/Dashboard/Render.fs` to render viewport slices, sticky identifying columns, horizontal offsets, empty placeholders, and scroll indicators for all table surfaces
- [X] T020 [US1] Update full detail rendering and state transitions so long detail content scrolls without losing the selected feature, story, plan, task, or diagnostic context
- [X] T021 [US1] Capture US1 independent evidence in readiness artifacts by running semantic tests plus dashboard smoke navigation for large and wide tables

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 (US2)

### Tests First

- [X] T022 [P] [US2] Add Core semantic tests for table border preference parsing, validation, defaults, unknown fields, and diagnostic reporting
- [X] T023 [P] [US2] Add Core semantic tests for detail formatting and color role parsing, safe defaults, invalid colors, and low-readability pair fallback
- [X] T024 [P] [US2] Add Dashboard rendering smoke tests proving every table surface applies `none`, `minimal`, `rounded`, and `heavy` borders consistently
- [X] T025 [P] [US2] Add Dashboard rendering smoke tests for readable detail headings, labels, status values, metadata, body text, warnings, errors, and source text

### Implementation

- [X] T026 [US2] Extend the shared dashboard settings model and JSON parser in Core for `ui.table`, `ui.detail`, `ui.colors`, and per-field diagnostics
- [X] T027 [US2] Map supported border preferences to Spectre.Console table border variants and apply the chosen style to feature, story, task, diagnostic, settings, and detail-oriented tables
- [X] T028 [US2] Apply configurable detail formatting and color roles to all full detail render paths while preserving safe fallbacks for invalid settings
- [X] T029 [US2] Surface configuration diagnostics in the dashboard without crashing or clearing the last valid display settings
- [X] T030 [US2] Capture US2 independent evidence with config fixtures, semantic tests, rendered smoke output for each supported border and detail role fallback, and maintainer readability review results in `specs/004-table-settings-controls/readiness/detail-readability-review.md`

**Checkpoint**: User Story 2 is fully functional and testable independently.

---

## Phase 5: User Story 3 (US3)

### Tests First

- [X] T031 [P] [US3] Add Core semantic tests for settings edit sessions, dirty state, loaded file versions, stale-save detection, reload, discard, and explicit overwrite
- [X] T032 [P] [US3] Add Core semantic tests for live reload state transitions, debounce values, last-valid settings retention, invalid config diagnostics, and deferred reloads for dirty sessions
- [X] T033 [P] [US3] Add Dashboard smoke tests for opening the in-dashboard settings page by hotkey within 2 seconds, preserving current selection, saving, discarding, and showing validation feedback
- [X] T034 [P] [US3] Add CLI smoke tests for `sk-dashboard --settings --config <path>` reading, validating, saving, and detecting stale config changes

### Implementation

- [X] T035 [US3] Register default hotkey bindings and command dispatch for `settings.open`, settings save/discard/reload/overwrite, table horizontal scroll, and detail scroll commands
- [X] T036 [US3] Implement the in-dashboard settings surface with editable sections for table behavior, borders, detail formatting, colors, hotkeys, and live reload
- [X] T037 [US3] Implement shared settings save/reload/discard/overwrite workflows with validation messages and stale-save conflict handling
- [X] T038 [US3] Add `sk-dashboard --settings` routing in `src/Dashboard/Program.fs` that uses the same config path and shared settings workflow as the running dashboard
- [X] T039 [US3] Implement debounced config file observation in the running dashboard with last-valid settings retention and clear diagnostics for invalid or unreadable changes
- [X] T040 [US3] Defer external reloads while a settings surface has unsaved edits and show pending conflict state until save, discard, reload, or overwrite
- [X] T041 [US3] Capture US3 independent evidence for hotkey settings, standalone settings mode, conflict handling, settings page open time under 2 seconds, locating and editing key settings under 60 seconds, and live reload applying valid changes within 2 seconds

**Checkpoint**: User Story 3 is fully functional and testable independently.

---

## Phase 6: Integration & Polish

- [X] T042 Refresh public surface baselines for all changed `.fsi` files and confirm Tier 1 contract additions are intentional
- [X] T043 Run `dotnet fantomas` or the repository's formatter on changed F# source and test files where available
- [X] T044 Run `dotnet test sk-Dashboard.sln` and capture the full test transcript in readiness artifacts
- [X] T045 Run quickstart smoke checks for dashboard launch, large-table navigation, detail scrolling, standalone settings mode, invalid config, and live reload
- [X] T046 Update `specs/004-table-settings-controls/quickstart.md` with any final command or interaction changes discovered during implementation
- [X] T047 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh --graph-only` and confirm no cycles, dangling refs, missing task ids, or unexpected propagation
- [X] T048 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh` and document a PASS verdict or every accepted synthetic-evidence override

---

## Synthetic-Evidence Inventory

List every `[S]` task here with its Principle IV disclosures. This section is
the source for the PR description's synthetic-evidence section.

| Task | Reason | Real-evidence path | Tracking issue |
|------|--------|---------------------|----------------|
| _(none yet)_ | | | |
