# Evidence Plan: Markdown Rendering and Constitution Hotkey

## Tier 1 Contract Surfaces

- `src/Core/Hotkeys.fsi`: adds the `constitution.open` command identifier and default `C` binding.
- `src/Core/Domain.fsi`: exposes Markdown document state, constitution view state, source locations, render statuses, diagnostics, and viewport reuse.
- `src/Dashboard/App.fs`: routes hotkey commands through user-reachable reducer transitions.
- `src/Dashboard/Render.fs`: renders Markdown-backed full/detail and constitution views while leaving compact table cells plain.
- `src/Dashboard/Input.fs`: keeps existing binding lookup and conflict behavior authoritative for the new command.

## Required Real Evidence

- Core semantic tests for command ids, default bindings, public state shapes, viewport clamping, and diagnostic status values.
- Dashboard smoke tests using real temporary filesystem repositories for Markdown views, compact tables, constitution open/close/scroll, failure states, and changed-on-reopen behavior.
- FSI transcript or host smoke run showing the new public command/state surface is reachable.
- Surface baseline refresh for changed `.fsi` files.
- Full restore, build, and test transcripts under this directory.

## Dependency Upgrade

- Upgrade `Spectre.Console` to a version compatible with `NTokenizers.Extensions.Spectre.Console` 2.2.0.
- Add `NTokenizers.Extensions.Spectre.Console` 2.2.0 only to `src/Dashboard/Dashboard.fsproj` because Markdown rendering is a terminal rendering concern.

## Synthetic Evidence

No synthetic evidence is planned. Any future synthetic-only task must update `tasks.md` with the required disclosure and tracking information before being marked `[S]`.
