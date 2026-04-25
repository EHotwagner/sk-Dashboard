# Contract: Markdown Rendering and Constitution Hotkey

## Public Core Contract

The Core public surface must expose only the state and commands required for tests and dashboard coordination.

Required additions or updates:
- `DashboardCommand` includes a constitution-open command with stable command id `constitution.open`.
- `Hotkeys.defaultBindings` includes `ConstitutionOpen` bound to `C`.
- `Hotkeys.commandId` returns a stable id for the new command.
- `Hotkeys.commandLabel` returns `Open constitution` for command discovery/help surfaces.
- Any public document or constitution view state is declared in `.fsi` before `.fs` implementation.
- Surface-area baseline tests are updated for intentional public additions.

Compatibility:
- Existing command ids and default bindings remain unchanged unless a conflict is explicitly resolved in tests.
- Existing hotkey preference loading, discovery, and conflict diagnostics apply to the new command.

## Dashboard Behavior Contract

### Open Constitution

Trigger: user presses the configured constitution command key, default `C`.

Expected behavior:
- Resolve the active repository root using existing project context.
- Read `.specify/memory/constitution.md` each time the command opens the view.
- Display content through the same Markdown rendering path used for full/detail document views.
- Preserve selected feature, story, task, focused pane, and navigation state.
- Show a non-fatal message if the file is missing, empty, unreadable, or cannot be rendered.

### Close Constitution

Trigger: existing close/back command while the constitution view is open.

Expected behavior:
- Close the constitution view.
- Restore the previous dashboard context without changing active selections.
- Preserve normal refresh and quit behavior.

### Scroll Constitution

Trigger: existing detail/document scroll commands while the constitution view is open.

Expected behavior:
- Move through long constitution content by keyboard.
- Clamp scroll offsets at document boundaries.
- Keep terminal layout usable in narrow or short terminals.

## Rendering Contract

Markdown formatting applies to:
- Constitution view.
- Full/detail document views that display Markdown-backed content.

Markdown formatting does not apply to:
- Compact dashboard table cells.
- Table headers or dense status columns unless they already use Spectre markup for dashboard styling.

Supported readable Markdown elements:
- Headings.
- Paragraphs.
- Ordered and unordered lists.
- Emphasis.
- Inline code.
- Links.
- Fenced code blocks.

Failure behavior:
- Unsupported or malformed Markdown falls back to readable escaped text where possible.
- Renderer exceptions create diagnostics and do not crash the dashboard.
