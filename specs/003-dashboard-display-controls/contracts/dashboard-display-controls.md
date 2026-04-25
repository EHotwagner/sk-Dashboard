# Contract: Dashboard Display Controls

## Header Version Contract

The dashboard header displays the application name followed by a version value.

Required behavior:

- The header includes `sk-dashboard` and a readable version label on every launch.
- The version is resolved from the installed application build metadata.
- If metadata is missing or unreadable, the label uses a clear fallback value such as `vunknown`.
- The version remains visible in both `widescreen` and `vertical` layouts.

## Preference File Additions

The existing global dashboard preferences file gains two color roles and four optional full-screen command bindings.

```json
{
  "version": 1,
  "bindings": [
    { "command": "fullscreen.feature", "key": "F" },
    { "command": "fullscreen.story", "key": "S" },
    { "command": "fullscreen.plan", "key": "P" },
    { "command": "fullscreen.task", "key": "T" }
  ],
  "ui": {
    "layout": "auto",
    "colors": {
      "rowStripeOdd": { "foreground": "white", "background": "#101820" },
      "rowStripeEven": { "foreground": "white", "background": "#18232f" }
    }
  }
}
```

### Stripe Color Roles

Required configurable roles:

- `rowStripeOdd`
- `rowStripeEven`

Accepted color value forms match the existing color contract:

- String named terminal color: `"grey"`
- String hex RGB color: `"#18232f"`
- Foreground/background pair:

```json
{ "foreground": "white", "background": "#18232f" }
```

Behavior:

- Missing stripe roles use built-in defaults.
- Unknown roles produce diagnostics and are ignored.
- Invalid color names and malformed hex values produce diagnostics and fall back for the affected role.
- Low-contrast foreground/background pairs produce diagnostics and fall back for the affected role.
- Selected, active, warning, and error row styles override stripe roles.

## Hotkey Command Contract

New command identifiers:

- `fullscreen.feature`
- `fullscreen.story`
- `fullscreen.plan`
- `fullscreen.task`

Behavior:

- Default bindings must be distinct from existing defaults.
- User-provided bindings override defaults through the existing preference-loading path.
- Invalid or conflicting bindings produce diagnostics and preserve safe defaults where required.
- Scripted smoke checks can invoke the commands through `--keys` using the active bindings.

## Full-Screen View Contract

Each full-screen command opens a modal view for exactly one target type.

### Feature View

Shows:

- Feature id, display name, branch, checkout state, artifact root, and artifact states.
- Source locations and source artifact text when available.
- Feature diagnostics.

Unavailable behavior:

- If no feature is selected, show a readable missing-feature message and keep the close command available.

### Story View

Shows:

- Story id, title, priority, description, acceptance scenarios, progress, related tasks, and source location.
- Associated source artifact text when available.
- Story-related diagnostics when available.

Unavailable behavior:

- If no story is selected or active, show a readable missing-story message.

### Plan View

Shows:

- Plan path, summary, technical context, constitution check, diagnostics, and raw plan source text.

Unavailable behavior:

- If no plan is loaded for the selected feature, show a readable missing-plan message.

### Task View

Shows:

- Task id, status, title, description, dependencies, related story, source location, metadata, diagnostics, and source artifact text when available.

Unavailable behavior:

- If no task is selected or active, show a readable missing-task message.

## Modal Navigation Contract

- Opening a full-screen view does not change selected feature, story, or task ids.
- Closing a full-screen view returns to the previous dashboard layout with the same selections preserved.
- Pressing another full-screen command while a modal is open replaces the modal target with the new target.
- Existing refresh, preference reload, and quit behavior remain available outside full-screen views.
