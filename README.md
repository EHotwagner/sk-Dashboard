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
| `enter` | Check out the selected feature branch |
| `r` | Refresh project state |
| `R` | Reload dashboard preferences |
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
sk-dashboard --refresh-interval 250 .
```

For scripted smoke checks, `--keys` accepts comma-separated key sequences using
the active bindings:

```bash
sk-dashboard --keys j,enter,down .
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

Example configuration:

```json
{
  "version": 1,
  "bindings": [
    { "command": "story.next", "key": "n" },
    { "command": "story.previous", "key": "p" }
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

Supported layout modes are `auto`, `widescreen`, and `vertical`. `auto` uses
the vertical layout below 120 terminal columns and the widescreen layout at
120 columns or wider.

Color values can be named terminal colors such as `green`, `yellow`, and
`deepskyblue1`, or hex RGB values such as `#7aa2f7`. Roles that use both text
and a background can be configured as `{ "foreground": "...", "background":
"..." }`.

Missing UI preferences use built-in defaults. Invalid colors, unsupported
layout modes, unknown color roles, and low-contrast foreground/background pairs
are reported in the dashboard diagnostics pane while safe defaults keep the
dashboard running. Press `R` to reload the preferences during a dashboard
session.
