# Contract: Theme Settings and Checklist Hotkey

## Public Core Contract

The Core public surface must expose the minimum state and functions needed for semantic tests, settings persistence, Dashboard coordination, and hotkey discovery.

Required additions or updates:
- `DashboardCommand` includes a checklist-open command with stable command id `checklists.open`.
- `Hotkeys.defaultBindings` includes the checklist command with default key sequence `L`, which is currently unassigned in dashboard scope.
- `Hotkeys.commandId`, command id parsing used by preference loading, and `Hotkeys.commandLabel` include the checklist command.
- Theme family, theme id, theme source, app theme, Markdown theme, resolved display mode, theme selection, and validation feedback types are declared in `.fsi` before implementation.
- Preference loading and saving exposes selected app and Markdown theme ids while preserving existing hotkey, layout, table, detail, and color preferences.
- Custom theme discovery returns built-in themes plus valid custom themes and diagnostics for invalid or ignored files.
- Surface-area baselines are updated for intentional public additions.

Compatibility:
- Existing command ids, settings fields, table/detail preferences, Markdown document state, and hotkey preference loading remain backward-compatible.
- Unknown future fields in theme definitions and preference files are tolerated when known values are valid.
- Missing custom themes fall back to built-in defaults without deleting the user's saved identifier unless the user saves a new selection.

## Theme Definition Contract

Custom theme files use JSON and include at least:

```json
{
  "family": "app",
  "id": "example-app-theme",
  "displayName": "Example App Theme",
  "version": 1
}
```

Custom theme folders:
- App themes: `<dashboard-user-config-dir>/themes/app/*.json`
- Markdown themes: `<dashboard-user-config-dir>/themes/markdown/*.json`
- Files are loaded in deterministic ordinal path order.
- Missing folders are treated as empty and do not produce startup failure.

For app themes, supported settings include:
- Light/dark mode behavior.
- Table border style, compact table roles, selection style, and alternate row shading.
- Foreground, background, muted, status, warning, error, success, accent, and panel colors.

For Markdown themes, supported settings include:
- Light/dark compatible element colors.
- Heading, emphasis, strong, link, inline code, code block, block quote, list marker, checklist item, note, normal, and muted roles.
- Spacing before/after sections, headings, paragraphs, lists, and code blocks.

Expected behavior:
- Files with the wrong `family` are ignored for the current theme family and reported as feedback.
- Files missing `id` or `displayName` are ignored.
- Duplicate ids do not replace the first valid theme in deterministic load order.
- Unknown properties are ignored.
- Invalid known values are rejected or replaced with safe fallback values and reported.

## Settings Behavior Contract

### Open Theme Settings

Trigger: existing settings command or settings-only mode.

Expected behavior:
- Show app theme choices: built-in `default`, `light`, `dark`, then valid custom app themes.
- Show Markdown theme choices: built-in `plain`, `default`, then valid custom Markdown themes.
- Show display names, current selection, fallback status, and validation feedback.
- Preserve previous dashboard context while settings are open.

### Save Theme Settings

Trigger: existing settings save command.

Expected behavior:
- Persist selected app and Markdown theme ids to the dashboard config.
- Apply safe live changes to visible dashboard surfaces within 2 seconds under normal local conditions.
- Preserve existing stale-save/conflict behavior.

### Discard Theme Settings

Trigger: existing settings discard/back command.

Expected behavior:
- Restore loaded app and Markdown theme selections.
- Keep the currently active last valid presentation.
- Return to the previous dashboard context.

## Rendering Contract

App themes apply to:
- Feature, story, task, diagnostic, settings, checklist, and detail table surfaces.
- Background, borders, selected rows, muted text, status, warning, error, success, and other compact dashboard roles.

Markdown themes apply to:
- Constitution view.
- Full/detail Markdown-backed document views.
- Checklist reading view.

Markdown themes do not apply document spacing to:
- Compact dashboard table cells.
- Dense status columns.
- Table headers, except for app theme colors and borders.

Failure behavior:
- Invalid custom app themes do not prevent built-in app themes from loading.
- Invalid custom Markdown themes do not prevent built-in Markdown themes from loading.
- If selected custom themes are unavailable, the app and Markdown families each fall back to their built-in `default` and show feedback.
- Renderer failures fall back to readable escaped/plain text where possible and produce diagnostics.

## Checklist Hotkey Contract

### Open Checklists

Trigger: user presses the configured checklist command key.

Expected behavior:
- Resolve the active feature from the dashboard context.
- Discover checklist files under the active feature's checklist location.
- If one or more checklists exist, open a checklist list view with keyboard-only navigation.
- If no checklists exist, show a non-fatal empty state.
- Preserve selected feature, story, task, focused pane, and navigation state.

### Read Checklist

Trigger: user selects a checklist from the checklist list.

Expected behavior:
- Read the checklist file from disk.
- Render headings, checked items, unchecked items, notes, and body text through the selected Markdown theme.
- Keep scroll offsets clamped and usable in narrow or short terminals.
- Show non-fatal feedback for missing, empty, unreadable, malformed, or very large checklist files.

### Close Checklists

Trigger: existing close/back command.

Expected behavior:
- Close the checklist view.
- Restore the prior dashboard context and selection.
- Preserve normal refresh, settings, and quit behavior.
