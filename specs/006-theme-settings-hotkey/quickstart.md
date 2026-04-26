# Quickstart: Theme Settings and Checklist Hotkey

## Prerequisites

- .NET 10 SDK available on `PATH`.
- Feature branch: `006-theme-settings-hotkey`.
- Repository contains a Spec Kit feature with `checklists/` files for checklist hotkey smoke checks.

## Build and Test

```bash
dotnet restore sk-Dashboard.sln
dotnet build sk-Dashboard.sln
dotnet test sk-Dashboard.sln
```

## Theme Smoke Check

1. Start the dashboard:

   ```bash
   dotnet run --project src/Dashboard -- .
   ```

2. Open in-app settings.
3. Confirm app theme choices include `default`, `light`, and `dark`.
4. Select `light`, save, and confirm tables, borders, status colors, selection, muted text, warnings, errors, and backgrounds switch to the light presentation.
5. Select `dark`, save, and confirm the same surfaces switch to the dark presentation.
6. Select app `default`, save, and confirm it resolves to a readable light or dark presentation based on app/environment preference or fallback.
7. Confirm alternate row shading remains off for built-in app themes unless explicitly enabled.
8. Confirm Markdown theme choices include `plain` and `default`.
9. Select Markdown `plain`, open a Markdown-backed detail/constitution/checklist view, and confirm it matches the current baseline as closely as possible.
10. Select Markdown `default`, open the same document, and confirm headings, emphasis, links, code, block quotes, lists, normal text, and spacing are easier to scan.

## Custom Theme Smoke Check

1. Add a valid custom app theme to the app theme folder.
2. Add a valid custom Markdown theme to the Markdown theme folder.
3. Restart or refresh settings as implementation requires.
4. Confirm both custom themes appear in the correct settings list with display names.
5. Select and save each custom theme.
6. Restart the dashboard and confirm selections are restored.
7. Remove the selected custom themes and restart or refresh.
8. Confirm the dashboard falls back to built-in defaults and shows non-fatal feedback.

## Invalid Theme Smoke Checks

Use temporary theme files or folders to verify:

- Missing theme folders still leave built-in themes available.
- Invalid JSON is ignored with feedback.
- Incomplete themes are ignored with feedback.
- Wrong-family themes are not listed for the current family.
- Duplicate theme ids do not replace the first valid theme.
- Unknown future fields do not prevent known valid settings from applying.
- Unreadable foreground/background combinations are rejected or safely replaced.

## Checklist Hotkey Smoke Check

1. Open the dashboard for a feature containing one or more files under `checklists/`.
2. Press `L`, the default checklist hotkey.
3. Confirm the checklist view opens in under 2 seconds and lists available checklists.
4. Open a checklist and confirm headings, checked items, unchecked items, and notes are readable through the selected Markdown theme.
5. Close the checklist view and confirm the previous dashboard selection and navigation context are restored.
6. Repeat with a feature that has no checklists and confirm a clear non-fatal empty state appears.

## Implementation Notes

- Add or update `.fsi` files before `.fs` implementations for public Core changes.
- Keep custom theme discovery and validation in Core when behavior needs semantic tests.
- Keep Spectre.Console rendering application in Dashboard.
- Keep compact table cells dense; document spacing belongs only to Markdown-backed full/detail/constitution/checklist views.
