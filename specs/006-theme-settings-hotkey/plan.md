# Implementation Plan: Theme Settings and Checklist Hotkey

**Branch**: `006-theme-settings-hotkey` | **Date**: 2026-04-26 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-theme-settings-hotkey/spec.md`

## Summary

Add two independent theme families to the existing F#/.NET terminal dashboard: app themes for compact TUI presentation and Markdown themes for readable document rendering. The implementation extends the current Core preference/settings contract with typed theme identifiers, discovery, validation, fallback, and persistence; Dashboard applies resolved app and Markdown theme values to tables, settings, document, constitution, and checklist surfaces. A new dedicated checklist hotkey opens available Spec Kit checklists for the active feature while preserving the prior dashboard context.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`; current project target)  
**Primary Dependencies**: Spectre.Console 0.54.0 for terminal rendering; `NTokenizers.Extensions.Spectre.Console` 2.2.0 for Markdown rendering already used by prior Markdown work; Expecto for Core semantic and Dashboard smoke tests; .NET IO and JSON APIs for theme/config discovery and persistence  
**Storage**: Existing dashboard user config path from `SpeckitArtifacts.resolveUserConfigPath ()` for saved selections; custom theme definition files under separate app-theme and Markdown-theme folders resolved relative to the user config directory, with optional repository-local theme folders if already supported by implementation context  
**Testing**: Expecto semantic tests through public `.fsi` surfaces for theme models, discovery, validation, fallback, persisted selection, hotkey command ids, and checklist state; Dashboard smoke tests for settings selection, live application, Markdown readability, invalid custom theme handling, and checklist open/read/empty flows  
**Target Platform**: Developer terminal sessions compatible with common local terminals and editor-integrated terminals, including Emacs vterm  
**Project Type**: Console/TUI application with reusable Core library  
**Performance Goals**: Theme changes that are safe to apply live become visible within 2 seconds; checklist hotkey opens within 2 seconds for active features containing up to 10 checklist files; custom theme discovery remains bounded to configured theme folders and does not block startup on invalid files  
**Constraints**: Keyboard-only operation remains supported; compact table cells remain dense and do not use Markdown document spacing; built-in app theme alternate row shading is off by default; invalid themes, unreadable theme folders, missing selected custom themes, and checklist failures produce non-fatal diagnostics while preserving the last usable presentation  
**Scale/Scope**: Single-user local dashboard; built-in `default`/`light`/`dark` app themes, built-in `plain`/`default` Markdown themes, custom themes from theme folders, in-app settings selection, persisted selections, Markdown-backed constitution/detail/checklist rendering, and a checklist hotkey for the active feature

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Specification exists and names user-visible behavior, scope boundaries, Tier 1 classification, public API impact, verification approach, edge cases, and measurable success criteria.
- PASS: This is a Tier 1 contracted change. Core `.fsi` signatures and public surface baselines must be updated for theme types, theme selection state, validation diagnostics, resolved display mode, checklist command/state, and any settings contract additions exposed beyond Dashboard.
- PASS: Plan keeps F#/.NET as the exclusive stack and reuses existing Spectre.Console, Markdown rendering, Expecto, JSON, and filesystem infrastructure. No new external dependency is planned.
- PASS: The feature follows Spec -> FSI -> semantic tests -> implementation. Public additions must be drafted in `.fsi`, exercised through FSI or semantic tests, and only then implemented in `.fs`.
- PASS: Idiomatic simplicity is preserved. Theme definitions are typed records/discriminated unions with small parser/validation functions; no custom operators, reflection, SRTP, type providers, or non-trivial computation expressions are planned.
- PASS: Synthetic evidence is not planned. Tests should use real temporary config/theme/checklist files and rendered dashboard output. Any synthetic-only evidence must follow `[S]` disclosure rules.
- PASS: Safe failure is explicit: theme folder/theme file/checklist IO failures, validation failures, and unreadable color combinations produce diagnostics and fallbacks without preventing startup or clearing the last usable presentation.

Post-design re-check: PASS. The design artifacts preserve the existing Core/Dashboard split, extend the established settings and hotkey surfaces, define theme/checklist contracts, and include real filesystem and rendering evidence paths. No constitution violations or complexity exceptions are required.

## Project Structure

### Documentation (this feature)

```text
specs/006-theme-settings-hotkey/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── theme-settings-checklist-hotkey.md
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
│   ├── ArtifactParsingTests.fs
│   ├── HotkeyTests.fs
│   └── Program.fs
└── Dashboard.Tests/
    ├── RenderingSmokeTests.fs
    ├── StateReducerTests.fs
    └── Program.fs
```

**Structure Decision**: Extend the existing Core/Dashboard split. Core owns typed theme contracts, selection persistence, custom theme discovery/validation results, resolved display mode, hotkey command identifiers, checklist artifact state, and diagnostics that need semantic tests through `.fsi`. Dashboard owns applying resolved app themes to Spectre.Console tables/panels/text, applying Markdown themes to Markdown-backed document rendering, in-app settings interaction, live refresh of visible surfaces, and smoke tests over rendered output.

## Dependency Decision

No new dependency is planned. Existing Spectre.Console rendering remains the terminal UI layer, and the Markdown renderer introduced by `005-markdown-constitution-hotkey` remains the Markdown document rendering path. Custom theme definitions should use the existing JSON/config parsing approach where practical so the dashboard does not add a second parser or configuration dependency.

## Complexity Tracking

No constitution violations or complexity exceptions are required.
