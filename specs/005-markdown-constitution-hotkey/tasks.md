# Tasks: Markdown Rendering and Constitution Hotkey

**Feature branch**: `005-markdown-constitution-hotkey`
**Spec**: `specs/005-markdown-constitution-hotkey/spec.md`
**Plan**: `specs/005-markdown-constitution-hotkey/plan.md`

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

- [X] T001 Confirm the feature branch, spec, plan, contracts, data model, quickstart, and requested NTokenizers package decision are present for `005-markdown-constitution-hotkey`
- [X] T002 [P] Create readiness scaffolding under `specs/005-markdown-constitution-hotkey/readiness/` for FSI transcripts, surface baselines, Markdown smoke captures, dashboard smoke logs, and failure-case transcripts
- [X] T003 [P] Record the Tier 1 contract surfaces, affected Core/Dashboard modules, dependency upgrade, and required real-evidence paths in `specs/005-markdown-constitution-hotkey/readiness/evidence-plan.md`
- [X] T004 Map existing hotkey registration, input dispatch, detail/fullscreen rendering, table rendering, active repository resolution, and dashboard context restoration paths to the feature requirements

## Phase 2: Foundation

- [X] T005 Draft public Core `.fsi` signatures for `ConstitutionOpen`, Markdown document state, constitution view state, source locations, render statuses, viewport state reuse, and render failure diagnostics
- [X] T006 [P] Add Core semantic test scaffolding for hotkey command ids, default bindings, public document/constitution state, viewport clamping, source paths, and diagnostic status values
- [X] T007 [P] Add Dashboard smoke test scaffolding for Markdown-rendered document views, compact plain table cells, constitution open/close/scroll flows, and constitution failure states
- [X] T008 Exercise the draft public signatures from FSI and capture the transcript in `specs/005-markdown-constitution-hotkey/readiness/fsi-session.txt`
- [X] T009 Record public surface baselines for changed `.fsi` modules in `specs/005-markdown-constitution-hotkey/readiness/surface-baseline.txt`
- [X] T010 Define user-facing fallback messages and operational diagnostics for malformed Markdown, renderer exceptions, missing constitution files, empty files, unreadable files, and attempted file paths
- [X] T011 Define reusable Markdown and repository fixtures covering headings, paragraphs, lists, emphasis, inline code, links, fenced code blocks, long lines, tables, malformed syntax, empty constitution files, missing constitution files, unreadable files, and changed-on-reopen content
- [X] T012 Run the foundation test scaffolds once and capture the expected failing or pending evidence in `specs/005-markdown-constitution-hotkey/readiness/foundation-test-baseline.txt`

**Checkpoint**: Foundation ready — story implementation may begin in parallel.

## Phase 3: User Story 1 (US1)

### Tests First (Principle I, Principle V)

- [X] T013 [P] [US1] Add Core semantic tests for Markdown document render status, source metadata, fallback status, empty-document status, and renderer diagnostic shapes
- [X] T014 [P] [US1] Add Dashboard smoke tests proving headings, lists, emphasis, inline code, links, and fenced code blocks render as structured terminal content in constitution and full/detail document views
- [X] T015 [P] [US1] Add Dashboard smoke tests proving compact feature, story, task, plan, and diagnostic table cells keep Markdown-like text plain and row-stable
- [X] T016 [P] [US1] Add Dashboard smoke tests proving malformed or unsupported Markdown and renderer failures fall back to readable text without crashing

### Implementation

- [X] T017 [US1] Upgrade `Spectre.Console` and add `NTokenizers.Extensions.Spectre.Console` 2.2.0 in `src/Dashboard/Dashboard.fsproj`, then restore to verify dependency compatibility
- [X] T018 [US1] Implement a small Dashboard Markdown rendering adapter around NTokenizers/Spectre that returns formatted output or escaped plain-text fallback plus diagnostics
- [X] T019 [US1] Route full/detail Markdown-backed document rendering through the new adapter in `src/Dashboard/Render.fs` while preserving existing detail layout, scroll offsets, and color roles
- [X] T020 [US1] Keep compact dashboard table rendering on the existing plain-text path so Markdown-like table cell content does not expand or alter row height
- [X] T021 [US1] Wire render failure diagnostics into existing dashboard diagnostic surfaces without exposing unrelated environment details or terminating the render loop
- [X] T022 [US1] Capture US1 independent evidence with Core semantic tests, Dashboard Markdown rendering smoke output, fallback smoke output, and compact-table plain-text captures under readiness artifacts

**Checkpoint**: User Story 1 is fully functional and testable independently.

## Phase 4: User Story 2 (US2)

### Tests First

- [X] T023 [P] [US2] Add Core semantic tests proving `constitution.open` is a stable command id, default binding is `C`, command discovery includes the label, and existing conflict/customization behavior applies
- [X] T024 [P] [US2] Add Dashboard reducer and smoke tests proving pressing the constitution command opens the current constitution, closing restores selected feature/story/task/focus/detail context, and normal refresh/quit behavior is preserved
- [X] T025 [P] [US2] Add Dashboard smoke tests proving long constitution content can be navigated by keyboard with clamped vertical and horizontal offsets in narrow and short terminal layouts

### Implementation

- [X] T026 [US2] Add `ConstitutionOpen` to Core hotkey command types, command id parsing/serialization, default bindings, help/discovery metadata, and public `.fsi` exposure
- [X] T027 [US2] Implement active repository constitution resolution for `.specify/memory/constitution.md` and ensure the file is read on every constitution-open command
- [X] T028 [US2] Add typed constitution view state and reducer transitions for open, scroll, and close while preserving and restoring previous dashboard context
- [X] T029 [US2] Wire input dispatch so the configured constitution hotkey opens the view and existing detail/document scroll and close commands operate while the view is active
- [X] T030 [US2] Render the constitution view through the shared Markdown rendering adapter with usable layout and controls in `src/Dashboard/Render.fs`
- [X] T031 [US2] Capture US2 independent evidence with hotkey semantic tests, open/close context restoration smoke logs, scroll smoke logs, and a manual or scripted dashboard transcript under readiness artifacts

**Checkpoint**: User Story 2 is fully functional and testable independently.

## Phase 5: User Story 3 (US3)

### Tests First

- [X] T032 [P] [US3] Add Dashboard smoke tests for missing, empty, unreadable, malformed, and changed-on-reopen constitution files using real temporary repository fixtures where the local filesystem supports them
- [X] T033 [P] [US3] Add Core and Dashboard tests proving constitution access and Markdown render diagnostics include the attempted source path where relevant, avoid unrelated environment details, and never crash the dashboard

### Implementation

- [X] T034 [US3] Implement non-fatal constitution fallback documents and messages for missing, empty, unreadable, and render-failed constitution content
- [X] T035 [US3] Ensure reopening the constitution after file changes discards stale content and displays the current file contents within the 2-second target for files up to 2,000 lines
- [X] T036 [US3] Harden constitution and detail rendering for long lines, large code blocks, nested lists, Markdown tables, and narrow or short terminals without overlapping controls or corrupting layout
- [X] T037 [US3] Capture US3 independent evidence with failure-case smoke transcripts, changed-on-reopen verification, no-crash diagnostics, and 2,000-line constitution timing logs under readiness artifacts

**Checkpoint**: User Story 3 is fully functional and testable independently.

## Phase 6: Integration & Polish

- [X] T038 Refresh public surface baselines for all changed `.fsi` files and confirm Tier 1 contract additions are intentional
- [X] T039 Run the repository formatter on changed F# source and test files where available, capturing output in `specs/005-markdown-constitution-hotkey/readiness/formatter.txt`
- [X] T040 Run `dotnet restore sk-Dashboard.sln`, `dotnet build sk-Dashboard.sln`, and `dotnet test sk-Dashboard.sln`, capturing full transcripts in readiness artifacts
- [X] T041 Run quickstart smoke checks for dashboard launch, Markdown detail rendering, compact plain table cells, constitution hotkey open/scroll/close, missing/empty/unreadable constitution cases, malformed Markdown fallback, and changed-on-reopen behavior
- [X] T042 Update `specs/005-markdown-constitution-hotkey/quickstart.md` and contract notes with any final command, dependency, or interaction details discovered during implementation
- [X] T043 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh specs/005-markdown-constitution-hotkey --graph-only` and confirm no cycles, dangling refs, missing task ids, or unexpected propagation
- [X] T044 Run `.specify/extensions/evidence/scripts/bash/run-audit.sh specs/005-markdown-constitution-hotkey` and document a PASS verdict or every accepted synthetic-evidence override

---

## Synthetic-Evidence Inventory

List every `[S]` task here with its Principle IV disclosures. This section is
the source for the PR description's synthetic-evidence section.

| Task | Reason | Real-evidence path | Tracking issue |
|------|--------|---------------------|----------------|
| _(none yet)_ | | | |
