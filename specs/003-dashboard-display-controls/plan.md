# Implementation Plan: Dashboard Display Controls

**Branch**: `003-dashboard-display-controls` | **Date**: 2026-04-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-dashboard-display-controls/spec.md`

## Summary

Add dashboard display controls that make the running build version visible, improve table readability with configurable alternating row backgrounds, and provide keyboard-opened modal full-screen views for the current feature, user story, plan, or task. The implementation extends the existing F#/.NET terminal dashboard, reuses the current preferences and hotkey model, adds row stripe color roles with safe defaults, and introduces a transient full-screen modal state that preserves the selected feature, story, and task when closed.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`; current project target)  
**Primary Dependencies**: Spectre.Console for terminal rendering, colors, panels, tables, and modal-like full-screen renderables; Expecto for tests; .NET JSON, IO, and assembly/package metadata APIs  
**Storage**: Existing global dashboard preferences file at the resolved user config path; no repository-local persistent storage required  
**Testing**: Expecto semantic tests through public `.fsi` surfaces for version resolution, preference parsing, color validation, row striping precedence, command mapping, state reducer behavior, and rendering smoke checks for full-screen views  
**Target Platform**: Developer terminal sessions compatible with common editor-integrated terminals, including Emacs vterm  
**Project Type**: Console/TUI application with reusable core library  
**Performance Goals**: Version resolution is cached or constant-time per render; row striping adds no measurable startup cost and remains within the existing render cycle; full-screen view creation for loaded artifacts completes within one render cycle for typical Speckit feature files  
**Constraints**: Must preserve keyboard-only operation; must not fail startup on invalid preferences or missing source artifact text; selected/active/warning/error row states override stripes; modal full-screen views show exactly one target type and preserve selected feature/story/task on close; full-screen hotkeys participate in configurable bindings  
**Scale/Scope**: Single-user local dashboard; applies to feature, story, task, diagnostic, and detail-like tables; four full-screen target types (`feature`, `story`, `plan`, `task`); adds at least two configurable row stripe color roles

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Specification exists and captures user-visible behavior, scope, entities, assumptions, and measurable success criteria.
- PASS: This is a Tier 1 contracted change because it extends observable dashboard behavior, hotkey commands, preference roles, and public domain types; `.fsi` signatures and surface baselines must be updated where public Core types/functions change.
- PASS: Plan keeps F#/.NET as the exclusive stack and uses existing dependencies; no new dependency is planned.
- PASS: The feature follows Spec -> FSI -> semantic tests -> implementation. Planned public additions include dashboard command cases, color roles, version/full-screen model types, and rendering/state helpers exposed through `.fsi` only where needed.
- PASS: Synthetic evidence is not planned. Terminal smoke evidence should use real repository fixtures or the existing smoke project; any synthetic-only fallback must follow `[S]` disclosure rules.
- PASS: Safe failure is required: unavailable version metadata, missing source text, invalid stripe colors, and unavailable full-screen targets produce fallback text or diagnostics rather than startup failure.

Post-design re-check: PASS. The design artifacts preserve the existing Core/Dashboard split, avoid new dependencies, make public surface changes explicit, and define real test evidence for the new display behavior.

## Project Structure

### Documentation (this feature)

```text
specs/003-dashboard-display-controls/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── dashboard-display-controls.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── Core/
│   ├── Domain.fsi
│   ├── Domain.fs
│   ├── Hotkeys.fsi
│   ├── Hotkeys.fs
│   ├── SpeckitArtifacts.fsi
│   └── SpeckitArtifacts.fs
└── Dashboard/
    ├── App.fs
    ├── Input.fs
    ├── Program.fs
    └── Render.fs

tests/
├── Core.Tests/
│   ├── HotkeyTests.fs
│   ├── ArtifactParsingTests.fs
│   └── Program.fs
└── Dashboard.Tests/
    ├── StateReducerTests.fs
    ├── RenderingSmokeTests.fs
    └── Program.fs
```

**Structure Decision**: Extend the existing Core/Dashboard split. Version, row stripe roles, full-screen target types, and command identifiers belong in `src/Core` when they affect preference parsing or public state. Input handling, modal state transitions, and Spectre.Console rendering belong in `src/Dashboard`, where terminal width and renderables are already handled.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
