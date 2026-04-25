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

The dashboard accepts an optional project path. When omitted, it reads the
current working directory. Startup attempts to check out the latest feature
branch unless `--no-auto-checkout` is supplied.

Useful options:

```bash
dotnet run --project src/Dashboard -- --no-auto-checkout .
dotnet run --project src/Dashboard -- --config ~/.config/sk-dashboard/hotkeys.json .
dotnet run --project src/Dashboard -- --refresh-interval 250 .
```

For scripted smoke checks, `--keys` accepts comma-separated key sequences using
the active bindings:

```bash
dotnet run --project src/Dashboard -- --keys K,enter,j .
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
