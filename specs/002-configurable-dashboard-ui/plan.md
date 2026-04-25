# Implementation Plan: Configurable Dashboard UI

**Branch**: `002-configurable-dashboard-ui` | **Date**: 2026-04-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-configurable-dashboard-ui/spec.md`

## Summary

Add user-configurable dashboard presentation settings to the existing Speckit terminal dashboard. The implementation extends the current single dashboard preferences file so it can contain both hotkey bindings and UI settings, including configurable color roles and layout mode. Layout supports `widescreen`, `vertical`, and `auto`; `auto` chooses vertical below 120 terminal columns and widescreen at 120 or more columns. Invalid colors, unsupported layout modes, and low-contrast foreground/background pairs are surfaced as recoverable diagnostics while safe defaults keep the dashboard usable.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`; current project target)  
**Primary Dependencies**: Spectre.Console for terminal rendering and color/style parsing; Expecto for tests; .NET JSON and file APIs for preferences  
**Storage**: Existing global dashboard preferences file at the resolved user config path; no repository-local persistent storage required  
**Testing**: Expecto unit tests for preference parsing, color validation, contrast fallback, layout-mode selection, and renderer behavior; dashboard smoke tests for narrow and wide layouts  
**Target Platform**: Developer terminal sessions compatible with common editor-integrated terminals, including Emacs vterm  
**Project Type**: Console/TUI application with reusable core library  
**Performance Goals**: Preference loading adds no more than 100 ms to dashboard startup on a repository with existing feature artifacts; layout decision completes within one render cycle; preference reload updates the visible dashboard on the next live refresh  
**Constraints**: Must preserve keyboard-only operation; must not fail startup on invalid preferences; must preserve selected feature/story/task state across layout and preference changes; must keep all primary section headers readable below 120 columns in automatic vertical layout  
**Scale/Scope**: Single-user local dashboard preferences; at least 9 configurable visual roles; three layout modes (`widescreen`, `vertical`, `auto`)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Specification exists and captures user-visible behavior, scope, entities, and measurable success criteria.
- PASS: This is a Tier 1 contracted change because it extends user-facing configuration and observable dashboard behavior; `.fsi` signatures and public surface baseline must be updated where core preference types/functions change.
- PASS: Plan keeps F#/.NET as the exclusive stack and uses existing dependencies; no new dependency is planned.
- PASS: Semantic tests are required for the public preference-loading surface before implementation.
- PASS: Synthetic evidence is not planned. Any future synthetic-only terminal rendering evidence must follow the `[S]` disclosure rules.
- PASS: Safe failure is central to the feature: invalid preferences become diagnostics and defaults, not startup failures.

Post-design re-check: PASS. The design artifacts preserve the same architecture, avoid new dependencies, and keep public preference changes explicit and testable.

## Project Structure

### Documentation (this feature)

```text
specs/002-configurable-dashboard-ui/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── dashboard-ui-preferences.md
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
│   └── Hotkeys.fs
└── Dashboard/
    ├── Program.fs
    ├── Render.fs
    └── Input.fs

tests/
├── Core.Tests/
│   └── HotkeyTests.fs
└── Dashboard.Tests/
    ├── StateReducerTests.fs
    └── RenderingSmokeTests.fs
```

**Structure Decision**: Extend the existing Core/Dashboard split. Preference data and validation belong in `src/Core` so they can be tested independently and exposed through `.fsi`; layout selection and rendering application belong in `src/Dashboard` where terminal dimensions and Spectre renderables are available.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
