# Quickstart: Dashboard Display Controls

## Build

```bash
dotnet build sk-Dashboard.sln
```

## Run Tests

```bash
dotnet test sk-Dashboard.sln
```

## Run Dashboard

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- .
```

Expected checks:

- Header shows `sk-dashboard` followed by a version label.
- Feature, user story, task, diagnostic, and detail-style tables use alternating row backgrounds for adjacent non-selected rows.
- Selected, active, warning, and error rows remain visually distinct from stripe colors.

## Test Custom Stripe Preferences

Create or update the dashboard preference file at the configured path, for example `~/.config/sk-dashboard/hotkeys.json`:

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

Run the dashboard and press `R` to reload preferences during the session.

## Full-Screen Smoke Check

```bash
dotnet run --project src/Dashboard/Dashboard.fsproj -- --keys F,esc,S,esc,P,esc,T,esc .
```

Expected checks:

- `F` opens a feature full-screen modal.
- `S` opens a user story full-screen modal.
- `P` opens a plan full-screen modal.
- `T` opens a task full-screen modal.
- `esc` closes each modal and returns to the normal dashboard with feature, story, and task selections preserved.

## Package Update Check

Before installing this feature as a global tool, bump the package version in `Directory.Build.props`, then pack and update:

```bash
dotnet pack src/Dashboard/Dashboard.fsproj -c Release -o ~/.local/share/nuget-local
dotnet tool update -g sk-Dashboard --add-source ~/.local/share/nuget-local
sk-dashboard .
```

Expected check: the header version matches the updated installed package version.
