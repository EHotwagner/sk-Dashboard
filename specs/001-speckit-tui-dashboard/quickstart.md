# Quickstart: Speckit TUI Dashboard

## Prerequisites

- .NET SDK 10.x.
- Git available on `PATH`.
- A terminal compatible with common keyboard input and text rendering; Emacs vterm is a target environment.

## Build

```bash
dotnet restore
dotnet build
```

## Run

```bash
dotnet run --project src/Dashboard -- .
```

Useful options:

```bash
dotnet run --project src/Dashboard -- --no-auto-checkout .
dotnet run --project src/Dashboard -- --config ~/.config/sk-dashboard/hotkeys.json .
dotnet run --project src/Dashboard -- --refresh-interval 250 .
dotnet run --project src/Dashboard -- --keys K,enter,j .
```

Expected behavior:
- If no Speckit artifacts exist, the dashboard opens to an empty but navigable state.
- If feature branches exist, the dashboard attempts to check out the latest feature branch at startup.
- If checkout fails, the dashboard shows the failure and keeps rendering available context.

## Test

```bash
dotnet test
```

Test focus:
- Feature branch ordering and checkout error handling.
- Spec, plan, task, and checklist artifact discovery.
- User-story and task parsing from partial artifacts.
- Task graph dependency-chain selection, missing references, and cycle diagnostics.
- Hotkey conflict validation and global config loading.
- Dashboard state reducer behavior during refresh and resize events.

## Hotkey Configuration

Default bindings are available without configuration. User overrides are loaded from the global user configuration path:

```text
$XDG_CONFIG_HOME/sk-dashboard/hotkeys.json
```

When `XDG_CONFIG_HOME` is not available, the implementation uses
`~/.config/sk-dashboard/hotkeys.json` on typical Unix systems or the .NET
application data folder as fallback. Invalid or conflicting bindings are
reported in the dashboard and are not activated.

Example:

```json
{
  "version": 1,
  "bindings": [
    { "command": "story.next", "key": "n" },
    { "command": "story.previous", "key": "p" }
  ]
}
```

## Manual Verification Scenarios

1. Run in an empty directory and confirm the dashboard opens in under 3 seconds.
2. Create several feature branches and confirm startup selects the latest ordered feature.
3. Add partial `spec.md`, `plan.md`, and task artifacts and confirm missing files are shown as status rather than errors.
4. Create task dependencies across user stories and confirm the selected story graph includes dependency-chain tasks.
5. Introduce a missing task dependency and a cycle and confirm diagnostics are visible while recoverable tasks remain inspectable.
6. Resize the terminal to a compact size and confirm all panes remain reachable by keyboard.
