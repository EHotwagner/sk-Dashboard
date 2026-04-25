# Contract: Dashboard UI Preferences

## Preference File

The dashboard reads one global dashboard preferences file. The same file contains existing hotkey bindings and optional UI preferences.

## Top-Level Shape

```json
{
  "version": 1,
  "bindings": [
    { "command": "story.next", "key": "down" }
  ],
  "ui": {
    "layout": "auto",
    "colors": {
      "selected": { "foreground": "black", "background": "green" },
      "lastActivity": { "foreground": "white", "background": "#555555" },
      "progressComplete": "green",
      "progressIncomplete": "grey",
      "diagnosticInfo": "deepskyblue1",
      "diagnosticWarning": "yellow",
      "diagnosticError": "red",
      "muted": "grey",
      "panelAccent": "#7aa2f7"
    }
  }
}
```

## Layout Contract

Valid `ui.layout` values:

- `auto`
- `widescreen`
- `vertical`

Behavior:

- Missing `ui.layout` defaults to `auto`.
- `auto` uses vertical layout below 120 terminal columns.
- `auto` uses widescreen layout at 120 or more terminal columns.
- Unsupported values produce a visible diagnostic and fall back to `auto`.

## Color Contract

Required configurable roles:

- `selected`
- `lastActivity`
- `progressComplete`
- `progressIncomplete`
- `diagnosticInfo`
- `diagnosticWarning`
- `diagnosticError`
- `muted`
- `panelAccent`

Accepted color value forms:

- String named terminal color: `"green"`
- String hex RGB color: `"#7aa2f7"`
- Foreground/background pair:

```json
{ "foreground": "white", "background": "#555555" }
```

Behavior:

- Missing roles use built-in defaults.
- Unknown roles produce diagnostics and are ignored.
- Invalid color names and malformed hex values produce diagnostics and fall back for the affected role.
- Low-contrast foreground/background pairs produce diagnostics and fall back for the affected role.
- Valid preferences continue to apply when other roles are invalid.

## Reload Contract

When preferences reload during a running dashboard session:

- Valid hotkey changes apply.
- Valid UI changes apply.
- Invalid values produce diagnostics.
- Selected feature, story, and task remain selected when still valid.
- Dashboard startup and live reload do not fail solely because UI preferences are invalid.
