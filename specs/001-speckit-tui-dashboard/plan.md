# Implementation Plan: Speckit TUI Dashboard

**Branch**: `001-speckit-tui-dashboard` | **Date**: 2026-04-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-speckit-tui-dashboard/spec.md`

## Summary

Build a keyboard-first terminal dashboard for local Speckit repositories. The implementation will be an F#/.NET 10 console application using Spectre.Console for terminal layout, rendering, and live updates, with a separate core library for Speckit artifact discovery, Git branch selection, user-story parsing, task graph construction, status reporting, and persistent hotkey configuration.

The dashboard must be robust from an empty project state through complete feature artifacts. Startup attempts to identify and check out the latest feature branch, reports checkout failures visibly, and continues rendering recoverable project state.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`; local SDK verified as `10.0.104`)  
**Primary Dependencies**: Spectre.Console for TUI rendering/live layout; Expecto for tests; .NET file system watchers and process APIs for artifact and Git state observation  
**Storage**: Local repository files plus global user hotkey config at an XDG-compatible user config path, with platform fallback to .NET special folders  
**Testing**: Expecto unit tests for parsers, branch ordering, graph validation, config validation, and state reducers; integration tests with temporary Git/Speckit worktrees; console rendering smoke tests for small terminal sizes  
**Target Platform**: Developer terminal sessions compatible with common editor-integrated terminals, including Emacs vterm  
**Project Type**: Console/TUI application with reusable core library  
**Performance Goals**: Empty project stable state under 3 seconds; latest feature status for 10 branches under 5 seconds; file/branch update reflected within 2 seconds for normal local changes  
**Constraints**: Must not exit on missing/malformed Speckit artifacts; must preserve keyboard-only operation; must show raw task status text without normalization; must clearly report checkout and graph errors; must remain usable in constrained terminal sizes  
**Scale/Scope**: Local repositories with at least 10 feature branches, 5 user stories, and 30 tasks per feature; designed for single-user local operation rather than multi-user service use

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution is still the generated placeholder and contains no concrete enforceable gates. This plan therefore applies the active Spec Kit requirements directly:

- PASS: The implementation is planned as a local, keyboard-first console application matching the requested user workflow.
- PASS: Core behavior is separated from rendering so branch detection, artifact parsing, graph construction, and config validation can be independently tested.
- PASS: Missing and malformed artifacts are treated as normal recoverable states, matching the P1 reliability requirements.
- PASS: No unjustified complexity exceptions are introduced.

Post-design re-check: PASS. The generated research, data model, contracts, and quickstart preserve the same architecture and no constitution violations were introduced.

## Project Structure

### Documentation (this feature)

```text
specs/001-speckit-tui-dashboard/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── dashboard-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── Core/
│   ├── Core.fsproj
│   ├── Domain.fsi
│   ├── Domain.fs
│   ├── SpeckitArtifacts.fsi
│   ├── SpeckitArtifacts.fs
│   ├── GitFeatures.fsi
│   ├── GitFeatures.fs
│   ├── TaskGraph.fsi
│   ├── TaskGraph.fs
│   ├── Hotkeys.fsi
│   └── Hotkeys.fs
└── Dashboard/
    ├── Dashboard.fsproj
    ├── Program.fs
    ├── App.fs
    ├── Render.fs
    └── Input.fs

tests/
├── Core.Tests/
│   ├── Core.Tests.fsproj
│   ├── ArtifactParsingTests.fs
│   ├── GitFeatureTests.fs
│   ├── TaskGraphTests.fs
│   └── HotkeyTests.fs
└── Dashboard.Tests/
    ├── Dashboard.Tests.fsproj
    ├── StateReducerTests.fs
    └── RenderingSmokeTests.fs
```

**Structure Decision**: Replace the current placeholder library with a reusable `src/Core` library and add `src/Dashboard` as the console entry point. Tests split along the same boundary so project state logic can be tested without terminal rendering.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
