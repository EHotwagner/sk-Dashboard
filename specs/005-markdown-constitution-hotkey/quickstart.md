# Quickstart: Markdown Rendering and Constitution Hotkey

## Prerequisites

- .NET 10 SDK available on `PATH`.
- Repository contains `.specify/memory/constitution.md`.
- Feature branch: `005-markdown-constitution-hotkey`.

## Build and Test

```bash
dotnet restore sk-Dashboard.sln
dotnet build sk-Dashboard.sln
dotnet test sk-Dashboard.sln
```

## Manual Smoke Check

1. Ensure `.specify/memory/constitution.md` contains headings, lists, links, inline code, and a fenced code block.
2. Start the dashboard:

   ```bash
   dotnet run --project src/Dashboard -- .
   ```

3. Press `C`.
4. Confirm the constitution opens in a formatted readable view.
5. Scroll through the full document using `up`/`down` or `u`/`v`; use `left`/`right` or shifted horizontal detail keys for horizontal movement where configured.
6. Close the view and confirm the prior feature/story/task context is restored.
7. Edit `.specify/memory/constitution.md`, reopen the constitution view with `C`, and confirm the changed content appears.

## Failure Smoke Checks

Run the dashboard against temporary repositories or temporary constitution states:

- Missing `.specify/memory/constitution.md`: pressing `C` shows a non-fatal unavailable message.
- Empty constitution file: pressing `C` shows an explicit empty-document message.
- Unreadable constitution file where supported by the local filesystem: pressing `C` shows a non-fatal read error with the attempted path.
- Malformed or unusual Markdown: pressing `C` keeps content readable and does not crash.

## Implementation Notes

- `src/Dashboard/Dashboard.fsproj` pins `Spectre.Console` 0.54.0 and `NTokenizers.Extensions.Spectre.Console` 2.2.0.
- The new Markdown renderer package is isolated to Dashboard; Core exposes only document state and command contracts.
- Keep compact table cell rendering plain.
- Add or update `.fsi` files before `.fs` implementations for public Core changes.
