# Implementation Plan: Table Settings Controls

**Branch**: `004-table-settings-controls` | **Date**: 2026-04-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-table-settings-controls/spec.md`

## Summary

Make every dashboard table and full detail surface navigable when content exceeds the terminal, add global table border and detail readability settings, and provide both in-dashboard and standalone settings experiences that edit the shared dashboard configuration with live reload. The implementation extends the existing F#/.NET terminal dashboard, builds on the current preference and hotkey model, adds scroll viewport state for table/detail surfaces, introduces typed display settings with safe validation defaults, and adds a `sk-dashboard --settings` mode that uses the same configuration file as the running dashboard.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`; current project target)  
**Primary Dependencies**: Spectre.Console 0.49.1 for terminal rendering, tables, panels, prompts/forms, and layout; Expecto for semantic and smoke tests; .NET JSON, IO, file metadata, and file watching APIs  
**Storage**: Existing user dashboard preference/configuration file at `SpeckitArtifacts.resolveUserConfigPath ()`; no repository-local persistent storage for runtime settings  
**Testing**: Expecto semantic tests through public `.fsi` surfaces for settings parsing/validation, scroll state reducers, conflict detection, hotkey mapping, live reload behavior, and rendering smoke tests for table/detail/settings surfaces  
**Target Platform**: Developer terminal sessions compatible with common local terminals and editor-integrated terminals, including Emacs vterm  
**Project Type**: Console/TUI application with reusable core library  
**Performance Goals**: Selection-anchored table scrolling stays within the existing render cycle for at least 500 visible-model rows; full detail scrolling handles at least 2,000 lines; valid config changes are detected and reflected within 2 seconds under normal local file conditions  
**Constraints**: Keyboard-only operation remains supported; invalid/unreadable config must never crash the dashboard; live reload must preserve active navigation and defer conflicts for dirty settings sessions; border styles are limited to `none`, `minimal`, `rounded`, and `heavy`; full detail and settings surfaces must preserve the selected feature/story/task context when opened and closed  
**Scale/Scope**: Single-user local dashboard; applies to feature, story, task, diagnostic, settings, and detail-oriented table surfaces; one shared config file; two settings surfaces, in-dashboard and `sk-dashboard --settings`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Specification exists and captures user-visible behavior, priorities, edge cases, entities, assumptions, and measurable success criteria.
- PASS: This is a Tier 1 contracted change because it extends observable dashboard behavior, CLI arguments, configuration contracts, hotkey commands, and public state/preferences types. `.fsi` signatures and surface baselines must be updated for public Core additions.
- PASS: Plan keeps F#/.NET as the exclusive stack and uses existing dependencies plus .NET runtime APIs; no new package dependency is planned.
- PASS: The feature follows Spec -> FSI -> semantic tests -> implementation. Planned public additions include table viewport state, detail viewport state, border style settings, detail color roles, settings edit session state, live reload state, and settings-mode command identifiers exposed through `.fsi` only where needed.
- PASS: Idiomatic simplicity is preserved. Stateful scroll offsets, file watcher debounce state, and conflict timestamps may use local `mutable` bindings where clearer; each use requires the constitution's one-line reason comment.
- PASS: Synthetic evidence is not planned. Smoke evidence should use real rendered dashboard output and real temporary config files. Any synthetic-only fallback must follow `[S]` disclosure rules.
- PASS: Safe failure is explicit: invalid border/color/hotkey/live-reload config falls back to last valid settings or defaults with diagnostics; stale settings saves require reload or explicit overwrite.

Post-design re-check: PASS. The design artifacts preserve the existing Core/Dashboard split, avoid new dependencies, define public contract changes, and include real filesystem/config evidence paths for live reload and conflict behavior.

## Project Structure

### Documentation (this feature)

```text
specs/004-table-settings-controls/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── table-settings-controls.md
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
    ├── Render.fs
    └── Program.fs

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

**Structure Decision**: Extend the existing Core/Dashboard split. Core owns typed settings, config parsing/validation, public command ids, conflict metadata, and state records that need semantic tests through `.fsi`. Dashboard owns table/detail viewport transitions, Spectre.Console table/border rendering, in-dashboard settings UI, standalone settings mode routing, live reload polling/watching, and interactive event handling.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
