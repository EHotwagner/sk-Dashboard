# sk-Dashboard

Keyboard-first terminal dashboard for local Speckit repositories. The dashboard
opens as a live Spectre.Console application, watches local Speckit artifacts,
and keeps feature, story, task, plan, and diagnostic context visible while you
work.

## Features

- Live terminal UI with configurable colored panels, tables, rulers, and
  progress bars.
- Automatic feature discovery from local `specs/` artifacts and Git branches.
- Startup selection of the latest feature branch, with visible checkout errors.
- User-story progress bars computed from story-scoped task completion.
- Task graph and detail panes scoped to the selected user story.
- Selection is preserved across refreshes when the selected items still exist.
- Last-activity highlighting marks the most recently active user story and task
  with a grey background.
- The header shows the running `sk-dashboard` version, and table rows use
  configurable alternating stripe backgrounds.
- Modal full-screen views expand the current feature, user story, plan, or task
  without changing the current selection.
- Large tables keep the selected row visible while navigating, and full-screen
  detail views support vertical and horizontal scrolling.
- Keyboard-only operation, including editor-integrated terminals such as Emacs
  vterm.

## Build

```bash
dotnet build sk-Dashboard.sln
```

## Test

```bash
dotnet test sk-Dashboard.sln
```

## Run

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- .
```

The dashboard accepts an optional project path. When omitted, it reads the
current working directory. Startup attempts to check out the latest feature
branch unless `--no-auto-checkout` is supplied.

## Controls

| Key | Action |
|-----|--------|
| `j` / `k` | Next / previous feature |
| `up` / `down` | Previous / next user story |
| `left` / `right` | Previous / next task |
| `h` / `l` | Scroll table content left / right |
| `enter` | Check out the selected feature branch |
| `r` | Refresh project state |
| `R` | Reload dashboard preferences |
| `F` | Open selected feature full screen |
| `S` | Open selected or active user story full screen |
| `P` | Open selected feature plan full screen |
| `T` | Open selected or active task full screen |
| `up` / `down` in full screen | Scroll full-screen detail up / down |
| `left` / `right` in full screen | Scroll full-screen detail left / right |
| `,` | Open dashboard settings |
| `1` / `2` / `3` / `4` in settings | Save / discard / reload / overwrite settings |
| `esc` | Close a full-screen view |
| `q` | Quit |

## Install As A Tool

Pack and install the dashboard globally from this checkout:

```bash
dotnet pack src/Dashboard/Dashboard.fsproj -c Release -o ~/.local/share/nuget-local
dotnet tool install -g sk-Dashboard --add-source ~/.local/share/nuget-local
```

After installation, run it from any Speckit repository:

```bash
sk-dashboard .
```

To update an existing installation from this checkout, bump the package version
in `Directory.Build.props`, pack, then update the tool:

```bash
dotnet pack src/Dashboard/Dashboard.fsproj -c Release -o ~/.local/share/nuget-local
dotnet tool update -g sk-Dashboard --add-source ~/.local/share/nuget-local
```

Useful options:

```bash
sk-dashboard --no-auto-checkout .
sk-dashboard --config ~/.config/sk-dashboard/hotkeys.json .
sk-dashboard --settings --config ~/.config/sk-dashboard/hotkeys.json
sk-dashboard --refresh-interval 250 .
```

For scripted smoke checks, `--keys` accepts comma-separated key sequences using
the active bindings:

```bash
sk-dashboard --keys j,enter,down .
sk-dashboard --keys F,esc,S,esc,P,esc,T,esc .
```

Arrow-key names are accepted in scripted checks: `up`, `down`, `left`, and
`right`.

## Layout

```text
src/Core          Speckit artifact, Git, task graph, and hotkey logic
src/Dashboard     Spectre.Console terminal application
tests/Core.Tests  Expecto coverage for core behavior
tests/Dashboard.Tests  Expecto coverage for dashboard state and rendering
```

## Dashboard Preferences

The global dashboard preferences path resolves to
`$XDG_CONFIG_HOME/sk-dashboard/hotkeys.json` when `XDG_CONFIG_HOME` is set,
otherwise to `~/.config/sk-dashboard/hotkeys.json` on typical Unix systems.
The same JSON file stores keyboard bindings and optional UI preferences.

You can edit that file directly, pass another path with `--config`, or open the
standalone settings view:

```bash
sk-dashboard --settings
sk-dashboard --settings --config ~/.config/sk-dashboard/hotkeys.json
```

When the dashboard is running, press `,` to open the in-dashboard settings
surface. In that surface, `h`/`l` cycles the table border, `shift+left` and
`shift+right` adjust the detail horizontal scroll step, `1` saves, `2`
discards, `3` reloads from disk, and `4` overwrites after a conflict.

The dashboard watches the config file while it is running. Valid changes are
applied live. Invalid JSON or invalid settings are reported in the diagnostics
pane while the last valid settings remain active.

Example configuration:

```json
{
  "version": 1,
  "bindings": [
    { "command": "story.next", "key": "n" },
    { "command": "story.previous", "key": "p" },
    { "command": "settings.open", "key": "," },
    { "command": "table.scrollLeft", "key": "h" },
    { "command": "table.scrollRight", "key": "l" },
    { "command": "detail.scrollUp", "key": "u" },
    { "command": "detail.scrollDown", "key": "v" }
  ],
  "ui": {
    "layout": "auto",
    "table": {
      "border": "rounded",
      "stickyColumns": 1,
      "horizontalStep": 8
    },
    "detail": {
      "wrapText": false,
      "horizontalStep": 8
    },
    "liveReload": {
      "enabled": true,
      "debounceMilliseconds": 250
    },
    "colors": {
      "selected": { "foreground": "black", "background": "green" },
      "lastActivity": { "foreground": "white", "background": "#555555" },
      "progressComplete": "green",
      "progressIncomplete": "grey",
      "diagnosticInfo": "deepskyblue1",
      "diagnosticWarning": "yellow",
      "diagnosticError": "red",
      "muted": "grey",
      "panelAccent": "#7aa2f7",
      "rowStripeOdd": { "foreground": "white", "background": "#101820" },
      "rowStripeEven": { "foreground": "white", "background": "#18232f" },
      "detailHeading": "deepskyblue1",
      "detailLabel": "yellow",
      "detailBody": "white",
      "detailSource": "grey"
    }
  }
}
```

Supported layout modes are `auto`, `widescreen`, and `vertical`. `auto` uses
the vertical layout below 120 terminal columns and the widescreen layout at
120 columns or wider.

Supported table borders are `none`, `minimal`, `rounded`, and `heavy`.
`ui.table.horizontalStep` controls how far `h`/`l` move wide table text.
`ui.detail.horizontalStep` controls full-screen detail horizontal scrolling.

Color values can be named terminal colors such as `green`, `yellow`, and
`deepskyblue1`, or hex RGB values such as `#7aa2f7`. Roles that use both text
and a background can be configured as `{ "foreground": "...", "background":
"..." }`.

Missing UI preferences use built-in defaults. Invalid colors, unsupported
layout modes, unknown color roles, and low-contrast foreground/background pairs
are reported in the dashboard diagnostics pane while safe defaults keep the
dashboard running. Press `R` to reload the preferences during a dashboard
session.

Stripe color roles use the same validation path as the other dashboard colors.
Invalid or low-contrast stripe colors are reported in diagnostics and replaced
with safe defaults for the affected role.
