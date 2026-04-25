# Implementation Plan: Markdown Rendering and Constitution Hotkey

**Branch**: `005-markdown-constitution-hotkey` | **Date**: 2026-04-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-markdown-constitution-hotkey/spec.md`

## Summary

Render Markdown-backed constitution and full/detail document surfaces with terminal formatting while keeping compact tables plain, and add a default `C` hotkey that opens the current project constitution without losing dashboard context. The implementation extends the existing F#/.NET terminal dashboard, adds a typed Markdown document/constitution-view state surface in Core where needed, upgrades Spectre.Console to the version required by `NTokenizers.Extensions.Spectre.Console`, and routes rendering failures through readable fallback text plus diagnostics.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`; current project target)  
**Primary Dependencies**: Spectre.Console for terminal rendering; `NTokenizers.Extensions.Spectre.Console` 2.2.0 for Markdown rendering; Expecto for semantic and smoke tests; .NET IO APIs for reading the current constitution file  
**Storage**: Reads `.specify/memory/constitution.md` from the active repository context on demand; no new persistent storage  
**Testing**: Expecto semantic tests through public `.fsi` surfaces for hotkey command ids and constitution/document state; Dashboard smoke tests for Markdown rendering, table plain-text behavior, missing/empty/unreadable constitution feedback, and context restoration  
**Target Platform**: Developer terminal sessions compatible with common local terminals and editor-integrated terminals, including Emacs vterm  
**Project Type**: Console/TUI application with reusable core library  
**Performance Goals**: Opening a constitution file up to 2,000 lines completes within 2 seconds under normal local filesystem conditions; Markdown render fallback is immediate enough for the existing dashboard render cycle  
**Constraints**: Keyboard-only operation remains supported; compact dashboard table cells remain plain; malformed Markdown, renderer failures, missing files, empty files, and unreadable files must not crash the dashboard; the current constitution must be re-read each time the view opens  
**Scale/Scope**: Single-user local dashboard; applies to constitution view and full/detail document views, not compact table cells

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Specification exists and names user-visible behavior, scope boundaries, Tier 1 classification, public API impact, safe failure cases, and measurable success criteria.
- PASS: This is a Tier 1 contracted change because it adds an observable hotkey command and changes rendering behavior. `.fsi` signatures and public surface baselines must be updated for any Core command/state additions.
- PASS: Plan keeps F#/.NET as the exclusive stack. One new package dependency is planned because the user specifically requested `NTokenizers.Extensions.Spectre.Console`; it requires upgrading Spectre.Console from 0.49.1 to a compatible version.
- PASS: The feature follows Spec -> FSI -> semantic tests -> implementation. Planned public additions include a constitution hotkey command, Markdown document state, constitution-view target/state, and render failure diagnostics only where public tests or consumers need them.
- PASS: Idiomatic simplicity is preserved. Markdown rendering is isolated behind a small Dashboard rendering adapter with fallback plain text; no custom operators, reflection, SRTP, or non-trivial computation expressions are planned.
- PASS: Synthetic evidence is not planned. Smoke evidence should use real temporary files and rendered dashboard output. Any synthetic-only fallback must follow `[S]` disclosure rules.
- PASS: Safe failure is explicit: constitution access and Markdown rendering errors produce non-fatal user feedback and structured diagnostics with the attempted file path where relevant.

Post-design re-check: PASS. The design artifacts preserve the existing Core/Dashboard split, isolate the new dependency in Dashboard rendering, define the public command/state contract, and include real filesystem/rendering verification paths.

## Project Structure

### Documentation (this feature)

```text
specs/005-markdown-constitution-hotkey/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── markdown-constitution-hotkey.md
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

**Structure Decision**: Extend the existing Core/Dashboard split. Core owns command identifiers, hotkey defaults, public document/view state, source paths, fallback status, and diagnostics that need semantic tests through `.fsi`. Dashboard owns file loading from the active repository, NTokenizers/Spectre Markdown rendering, full/detail/constitution view composition, input routing, and smoke tests over rendered output.

## Dependency Decision

`NTokenizers.Extensions.Spectre.Console` 2.2.0 requires `Spectre.Console >= 0.54.0`, while the dashboard currently references Spectre.Console 0.49.1. Implementation must upgrade Spectre.Console to a compatible pinned version in `src/Dashboard/Dashboard.fsproj` and run the existing rendering smoke tests to catch API or visual regressions. Maintenance owner: Dashboard rendering maintainers, because the dependency is isolated to terminal Markdown rendering.

Source consulted: NuGet package page for `NTokenizers.Extensions.Spectre.Console` 2.2.0 and the project website requested in the spec.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
