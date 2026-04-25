# Quickstart: Table Settings Controls

## Run Tests

```bash
dotnet test sk-Dashboard.sln
```

## Run The Dashboard

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- .
```

## Open Settings Mode

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- --settings --config /tmp/sk-dashboard-settings.json .
```

Expected behavior:

- The settings console opens instead of the dashboard.
- Saving writes to the same config file the dashboard reads.
- If the file changed after the settings console loaded it, save shows a conflict and requires reload or explicit overwrite.

## Smoke Check Large Tables

Use a repository fixture or feature directory with hundreds of tasks/stories, then run scripted keys through the dashboard.

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- --keys down,down,down,l,h .
```

Expected behavior:

- Selection remains visible as it moves beyond the initial table viewport.
- Horizontal scroll commands reveal hidden columns without losing identifying context.
- Border style changes apply to every rendered table.

## Example Config

Create or update the dashboard config file at the path passed to `--config` or the default resolved by the dashboard.

```json
{
  "version": 1,
  "bindings": [
    { "command": "settings.open", "key": "," },
    { "command": "table.scrollLeft", "key": "h" },
    { "command": "table.scrollRight", "key": "l" },
    { "command": "settings.save", "key": "1" }
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
      "detailHeading": "deepskyblue1",
      "detailLabel": "yellow",
      "detailBody": "white",
      "detailSource": "grey"
    }
  }
}
```

## Live Reload Check

1. Start the dashboard with `--config /tmp/sk-dashboard-settings.json`.
2. Change `ui.table.border` from `rounded` to `heavy` in that file.
3. Wait up to 2 seconds.

Expected behavior:

- The running dashboard applies the new table border style without restart.
- Invalid JSON or invalid settings keep the last valid settings and show a configuration diagnostic.
