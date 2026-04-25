# Existing Path Map

## Hotkeys and Input

- `src/Core/Hotkeys.fs` owns `DashboardCommand`, command id parsing, default bindings, preference loading, validation, and conflict diagnostics.
- `src/Dashboard/Input.fs` maps console keys to key sequences and resolves commands through the configured binding list.
- `src/Dashboard/App.fs` applies commands to dashboard state; full-screen targets already reuse `DetailViewport` for scrolling.

## Rendering

- `src/Dashboard/Render.fs` owns Spectre.Console renderables.
- Compact tables are built through `featuresTable`, `storiesTable`, `tasksTable`, and `diagnosticsRenderable`; they currently escape cell text through `Markup.Escape`.
- Full/detail document surfaces flow through `fullScreenText` and `fullScreenRenderable`.
- The dashboard detail pane uses `detailRenderable`.

## Repository and Constitution Resolution

- `src/Dashboard/App.fs` resolves the active repository via `SpeckitArtifacts.resolveRepositoryRoot`.
- Feature artifact roots are loaded into `DashboardSnapshot.Features[*].ArtifactRoot`.
- The project constitution source for this feature is `<repository root>/.specify/memory/constitution.md`.

## Context Restoration

- `FullScreenModal` stores selected feature, story, task, and viewport state.
- `App.closeFullScreen` clears only the modal and preserves the dashboard snapshot selections.
- `App.preserveSelections` retains selections during refresh and currently carries the existing full-screen modal forward.
