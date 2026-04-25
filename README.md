# sk-Dashboard

Keyboard-first terminal dashboard for local Speckit repositories.

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
dotnet run --project src/Dashboard -- .
```

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

The dashboard accepts an optional project path. When omitted, it reads the
current working directory. Startup attempts to check out the latest feature
branch unless `--no-auto-checkout` is supplied.

Useful options:

```bash
sk-dashboard --no-auto-checkout .
sk-dashboard --config ~/.config/sk-dashboard/hotkeys.json .
sk-dashboard --refresh-interval 250 .
```

For scripted smoke checks, `--keys` accepts comma-separated key sequences using
the active bindings:

```bash
sk-dashboard --keys K,enter,j .
```

## Layout

```text
src/Core          Speckit artifact, Git, task graph, and hotkey logic
src/Dashboard     Spectre.Console terminal application
tests/Core.Tests  Expecto coverage for core behavior
tests/Dashboard.Tests  Expecto coverage for dashboard state and rendering
```

The global hotkey config path resolves to
`$XDG_CONFIG_HOME/sk-dashboard/hotkeys.json` when `XDG_CONFIG_HOME` is set,
otherwise to `~/.config/sk-dashboard/hotkeys.json` on typical Unix systems.

Example hotkey configuration:

```json
{
  "version": 1,
  "bindings": [
    { "command": "story.next", "key": "n" },
    { "command": "story.previous", "key": "p" }
  ]
}
```
