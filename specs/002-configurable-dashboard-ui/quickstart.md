# Quickstart: Configurable Dashboard UI

## Goal

Verify that dashboard colors and layout mode can be configured, reloaded, and recovered safely when invalid values are present.

## Prerequisites

- .NET SDK 10.x.
- Existing dashboard app can run with `dotnet run --project src/Dashboard/Dashboard.fsproj -- .`.
- A Speckit repository with at least one feature and multiple user stories.

## Default Behavior

Run without UI preferences:

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- .
```

Expected:

- Dashboard opens with built-in colors.
- Layout mode behaves as `auto`.
- Below 120 columns, dashboard uses vertical layout.
- At 120 or more columns, dashboard uses widescreen layout.

## Configure UI Preferences

Create or edit the dashboard preferences file at the existing config path. The
same file contains `bindings` and optional `ui` settings.

Example:

```json
{
  "version": 1,
  "bindings": [
    { "command": "story.next", "key": "down" }
  ],
  "ui": {
    "layout": "vertical",
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

Run:

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- .
```

Expected:

- Dashboard uses vertical layout.
- Configured color roles appear in selected rows, last-activity markers, progress, diagnostics, muted text, and panel accents.
- Existing hotkey bindings still load from the same file.

## Reload Preferences

While the dashboard is running, update the preferences file and press `R`.

Expected:

- Valid UI changes apply without restart.
- Feature, story, and task selection remain stable when still valid.
- Diagnostics show invalid UI settings.

## Invalid Preference Recovery

Set one color to an invalid value and one foreground/background pair to a low-contrast combination.

Expected:

- Dashboard still opens.
- Valid settings apply.
- Invalid values produce visible diagnostics.
- Affected roles fall back to readable defaults.

## Layout Verification

Test `layout` values:

- `auto`
- `widescreen`
- `vertical`

Expected:

- `auto` chooses vertical below 120 columns and widescreen at 120 or more columns.
- `widescreen` keeps navigation and detail context side by side where space allows.
- `vertical` stacks primary sections top to bottom.

## Scripted Smoke Checks

From this repository, run:

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- --no-auto-checkout --config specs/002-configurable-dashboard-ui/readiness/us1-colors.json --keys q specs/002-configurable-dashboard-ui/readiness/smoke-project
dotnet run --project src/Dashboard/Dashboard.fsproj -- --no-auto-checkout --config specs/002-configurable-dashboard-ui/readiness/us2-vertical.json --keys down,right,q specs/002-configurable-dashboard-ui/readiness/smoke-project
dotnet run --project src/Dashboard/Dashboard.fsproj -- --no-auto-checkout --config specs/002-configurable-dashboard-ui/readiness/us3-invalid.json --keys n,q specs/002-configurable-dashboard-ui/readiness/smoke-project
```
