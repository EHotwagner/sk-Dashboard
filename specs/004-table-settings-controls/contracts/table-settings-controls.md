# Contract: Table Settings Controls

## CLI Contract

The existing dashboard executable gains a settings-only mode.

Required command:

```text
sk-dashboard --settings
```

Behavior:

- Opens a console settings experience instead of the main dashboard.
- Uses the same config path as the main dashboard, including `--config <path>` when provided.
- Validates draft settings before saving.
- Detects if the config file changed since the settings console loaded it.
- On stale save, requires reload or explicit overwrite before writing.

## Hotkey Command Contract

New command identifiers:

- `settings.open`
- `settings.save`
- `settings.discard`
- `settings.reload`
- `settings.overwrite`
- `table.scrollLeft`
- `table.scrollRight`
- `detail.scrollUp`
- `detail.scrollDown`
- `detail.scrollLeft`
- `detail.scrollRight`

Behavior:

- `settings.open` opens the in-dashboard settings page without changing selected feature/story/task context.
- Save/discard/reload/overwrite commands are scoped to settings surfaces.
- Horizontal table/detail scroll commands move explicit horizontal viewport state and do not change the selected row.
- Existing user-configured bindings continue to override defaults through the preference-loading path.
- Invalid or conflicting bindings produce diagnostics and preserve safe defaults where required.

## Preference File Contract

The shared dashboard JSON configuration supports the following known fields.

```json
{
  "version": 1,
  "bindings": [
    { "command": "settings.open", "key": "," },
    { "command": "table.scrollLeft", "key": "shift+left" },
    { "command": "table.scrollRight", "key": "shift+right" }
  ],
  "ui": {
    "layout": "auto",
    "table": {
      "borderStyle": "rounded",
      "horizontalScrollStep": 8
    },
    "detail": {
      "formattingEnabled": true
    },
    "colors": {
      "detailHeading": "deepskyblue1",
      "detailLabel": "cyan",
      "detailText": "white",
      "detailMuted": "grey",
      "detailSuccess": "green",
      "detailWarning": "yellow",
      "detailError": "red",
      "detailActive": { "foreground": "black", "background": "green" }
    }
  },
  "liveReload": {
    "enabled": true,
    "debounceMilliseconds": 250
  }
}
```

Validation:

- `ui.table.borderStyle` accepts only `none`, `minimal`, `rounded`, or `heavy`.
- Unknown future fields do not prevent known valid settings from loading.
- Unknown known-section fields produce diagnostics when useful but do not crash.
- Invalid color values or low-readability foreground/background pairs fall back per role.
- Invalid live reload values fall back to enabled reload with a safe debounce.

## Table Rendering Contract

Required behavior:

- Every dashboard table clamps visible rows to available terminal space and supports vertical scrolling when rows exceed that space.
- Selection changes keep the selected row visible when it still exists.
- Wide tables expose explicit left/right horizontal scrolling.
- Stable identifying columns remain visible where available.
- Border style preference applies consistently to feature, story, task, diagnostic, settings, and detail-oriented tables.

Fallback behavior:

- Empty tables render a readable placeholder row.
- Unsupported border styles use a readable default and report a configuration diagnostic.
- If a selected row disappears after refresh, selection and scroll state move to the nearest valid row or clear safely.

## Full Detail Contract

Required behavior:

- Full detail mode supports vertical scrolling for content longer than the visible area.
- Long source text or metadata supports horizontal scrolling.
- Opening and closing full detail mode preserves the selected feature, story, plan, task, or diagnostic context.
- Formatting visually separates headings, labels, status values, metadata, body text, warnings, errors, and source text.

Fallback behavior:

- Invalid detail formatting or colors fall back to safe defaults with diagnostics.
- Live config reload does not reset the user's current detail scroll position.

## Live Reload Contract

Required behavior:

- The running dashboard detects config file changes while open.
- Valid changes apply within 2 seconds under normal local file conditions.
- Invalid or unreadable changes keep the last valid settings and show diagnostics.
- Reloads avoid interrupting table navigation, full detail scrolling, and dirty settings edits.

Conflict behavior:

- A settings surface records the config version observed when opened.
- Before save, the surface compares the current config version with the loaded version.
- If stale, it shows a conflict warning and requires reload or explicit overwrite.
- While a settings surface has unsaved edits, external reloads for that surface are deferred and shown as pending conflict.
