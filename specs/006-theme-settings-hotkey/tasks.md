# Tasks: Theme Settings and Checklist Hotkey

**Feature branch**: `006-theme-settings-hotkey`
**Spec**: `specs/006-theme-settings-hotkey/spec.md`
**Plan**: `specs/006-theme-settings-hotkey/plan.md`

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
- **[US1]**, **[US2]**, ... — user-story scope
- **[T1]** / **[T2]** — Tier 1 (contracted) vs Tier 2 (internal) change

Every task must have a matching entry in `tasks.deps.yml` even if its
dependency list is empty. The `speckit.graph.compute` command refuses to
proceed with dangling references.

---

## Phase 1: Setup

- [X] T001 Create `specs/006-theme-settings-hotkey/readiness/` and seed placeholders for FSI, smoke, and audit transcripts
- [X] T002 [P] Record the current public Core surface baseline for `Domain.fsi`, `Hotkeys.fsi`, and `SpeckitArtifacts.fsi`
- [X] T003 [P] Add sample valid and invalid app/Markdown theme JSON files under readiness fixtures for implementation smoke checks
- [X] T004 [P] Add sample checklist fixtures covering non-empty, empty, missing, and malformed checklist inputs
- [X] T005 Document Tier 1 evidence obligations and quickstart commands in `readiness/evidence-plan.md`

---

## Phase 2: Foundation

- [X] T006 Draft theme family, theme id, theme source, app theme, Markdown theme, resolved display mode, selection, and validation feedback contracts in `src/Core/Domain.fsi`
- [X] T007 Draft checklist view state and context-preservation contracts in `src/Core/Domain.fsi` and active-feature checklist discovery signatures in `src/Core/SpeckitArtifacts.fsi`
- [X] T008 Draft checklist hotkey command contract in `src/Core/Hotkeys.fsi`, including stable id `checklists.open`, label, and default key `L`
- [X] T009 [P] Add shared Core test fixtures/builders for temporary dashboard config files, theme folders, and checklist folders
- [X] T010 Exercise the drafted `.fsi` additions from FSI and capture the transcript at `specs/006-theme-settings-hotkey/readiness/fsi-session.txt`
- [X] T011 Record unsupported-scope, fallback, and validation diagnostics expected for invalid themes, missing saved themes, and checklist IO failures
- [X] T012 Refresh intentional public surface baselines for the drafted Tier 1 Core additions

**Checkpoint**: Foundation ready — story implementation may begin in parallel.

---

## Phase 3: User Story 1 (US1) - Select App Theme Bundles

### Tests First

- [X] T013 [P] [US1] Add Core semantic tests for built-in app themes `default`, `light`, `dark`, display names, mode resolution, and alternate row shading default-off behavior
- [X] T014 [P] [US1] Add Core semantic tests for app theme selection persistence, fallback from missing selected app themes, and validation feedback
- [X] T015 [P] [US1] Add Dashboard smoke tests showing app theme choices in settings and rendered table/status/detail surfaces using selected theme roles

### Implementation

- [X] T016 [P] [US1] Implement app theme domain records, built-in `default`/`light`/`dark` definitions, color roles, table roles, and readable fallback mode in `src/Core/Domain.fs`
- [X] T017 [US1] Extend dashboard preference load/save in `src/Core/Hotkeys.fs` to preserve selected app theme id alongside existing bindings and UI preferences
- [X] T018 [US1] Apply resolved app themes to Dashboard settings, tables, panels, selected rows, status colors, muted text, warnings, errors, and backgrounds in `src/Dashboard/App.fs` and `src/Dashboard/Render.fs`
- [X] T019 [US1] Add live-safe app theme apply/discard behavior that preserves prior dashboard context and emits fallback diagnostics
- [X] T020 [US1] Capture US1 independent validation evidence in `readiness/app-theme-smoke.txt`

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 (US2) - Select Markdown Rendering Themes

### Tests First

- [X] T021 [P] [US2] Add Core semantic tests for built-in Markdown themes `plain` and `default`, element color roles, spacing rules, and app-mode compatibility
- [X] T022 [P] [US2] Add Dashboard smoke tests for Markdown theme selection in settings and themed constitution/detail Markdown rendering

### Implementation

- [X] T023 [P] [US2] Implement Markdown theme domain records, `plain` compatibility baseline, readable `default` theme, spacing clamps, and mode-compatible palettes
- [X] T024 [US2] Extend dashboard preference load/save to persist selected Markdown theme id independently from selected app theme id
- [X] T025 [US2] Apply selected Markdown themes to constitution and full/detail Markdown-backed views without expanding compact table cells, including live settings save/discard behavior within the 2-second target
- [X] T026 [US2] Add Markdown theme validation diagnostics for unreadable colors, excessive spacing, and renderer fallback paths
- [X] T027 [US2] Capture US2 independent validation evidence in `readiness/markdown-theme-smoke.txt`

**Checkpoint**: User Story 2 is fully functional and testable independently.

---

## Phase 5: User Story 3 (US3) - Use Custom Themes From Theme Folders

### Tests First

- [X] T028 [P] [US3] Add Core semantic tests for custom app and Markdown theme folder discovery, deterministic ordering, family separation, and display names
- [X] T029 [P] [US3] Add Core semantic tests for invalid JSON, incomplete themes, duplicate ids, wrong-family files, unreadable folders, and unknown future fields
- [X] T030 [P] [US3] Add Dashboard smoke tests for selecting custom themes, restart persistence, missing selected custom themes, and visible validation feedback

### Implementation

- [X] T031 [P] [US3] Implement family-specific custom theme folder resolution relative to the dashboard user config path
- [X] T032 [US3] Implement tolerant JSON custom theme parsing, validation, safe replacement of unreadable roles, and diagnostics in Core
- [X] T033 [US3] Merge built-in and valid custom themes into settings choices while keeping app and Markdown families separate
- [X] T034 [US3] Preserve saved custom theme identifiers across fallback until the user saves a new selection
- [X] T035 [US3] Capture US3 independent validation evidence in `readiness/custom-theme-smoke.txt`

**Checkpoint**: User Story 3 is fully functional and testable independently.

---

## Phase 6: User Story 4 (US4) - Open Spec Kit Checklists By Hotkey

### Tests First

- [X] T036 [P] [US4] Add Core semantic tests for checklist file discovery, checklist view state, empty-state diagnostics, and previous-context preservation
- [X] T037 [P] [US4] Add Hotkey tests for `ChecklistOpen`, command id `checklists.open`, label, default key `L`, conflict validation, and preference overrides
- [X] T038 [P] [US4] Add Dashboard smoke tests for pressing the checklist command, listing checklists, opening a checklist, keyboard navigation, and closing back to the prior context

### Implementation

- [X] T039 [P] [US4] Implement checklist discovery for active feature `checklists/` folders in `src/Core/SpeckitArtifacts.fs`
- [X] T040 [US4] Add the checklist command to Core hotkeys, Dashboard input routing, and `App.applyCommand`
- [X] T041 [US4] Implement checklist list/read/empty/error view state in `src/Dashboard/App.fs` while preserving feature, story, task, pane, and modal context
- [X] T042 [US4] Render checklist headings, checked items, unchecked items, notes, and empty/error messages through the selected Markdown theme in `src/Dashboard/Render.fs`
- [X] T043 [US4] Capture US4 independent validation evidence in `readiness/checklist-hotkey-smoke.txt`

**Checkpoint**: User Story 4 is fully functional and testable independently.

---

## Phase 7: Integration & Polish

- [X] T044 Run `dotnet format` or equivalent project formatting checks for modified F# files
- [X] T045 Run `dotnet build sk-Dashboard.sln` and capture build evidence
- [X] T046 Run `dotnet test sk-Dashboard.sln` and capture Core/Dashboard semantic and smoke evidence
- [X] T047 Run the quickstart theme, invalid-theme, custom-theme, and checklist hotkey smoke checks and capture transcripts under `readiness/`
- [X] T048 Refresh Tier 1 surface-area baselines and confirm intentional public additions only
- [X] T049 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh specs/006-theme-settings-hotkey --graph-only` and confirm no dangling refs or cycles
- [X] T050 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh specs/006-theme-settings-hotkey` and document every synthetic override if any are accepted

---

## Synthetic-Evidence Inventory

List every `[S]` task here with its Principle IV disclosures. This section is
the source for the PR description's synthetic-evidence section.

| Task | Reason | Real-evidence path | Tracking issue |
|------|--------|---------------------|----------------|
| _(none yet)_ | | | |
