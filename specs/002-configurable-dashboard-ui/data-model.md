# Data Model: Configurable Dashboard UI

## Dashboard Preference Set

User-owned dashboard configuration loaded from the existing global preferences file.

### Fields

- `bindings`: existing hotkey binding list.
- `ui`: optional UI preference set.

### Validation Rules

- Missing `ui` uses default UI preferences.
- Invalid `ui` children produce diagnostics but do not invalidate valid hotkey bindings.
- Preferences reload must preserve selected feature, story, and task state.

## UI Preference Set

Visual configuration subset inside the dashboard preference set.

### Fields

- `layout`: layout mode; valid values are `widescreen`, `vertical`, and `auto`.
- `colors`: map of color role names to color values or color pair values.

### Validation Rules

- Missing `layout` defaults to `auto`.
- Unsupported `layout` emits a diagnostic and falls back to `auto`.
- Missing `colors` uses default colors for all roles.
- Invalid color values affect only the role that contains the invalid value.

## Layout Mode

Named dashboard arrangement strategy.

### Values

- `widescreen`: side-by-side navigation and detail context.
- `vertical`: top-to-bottom sections for narrow or stacked terminals.
- `auto`: vertical below 120 terminal columns; widescreen at 120 or more columns.

### State Transitions

- User changes preference from any mode to any other valid mode.
- On live reload, dashboard applies the new mode without resetting feature/story/task selection.
- In `auto`, terminal width changes can switch the effective layout between vertical and widescreen.

## Color Role

Named visual purpose that can be customized.

### Required Roles

- `selected`
- `lastActivity`
- `progressComplete`
- `progressIncomplete`
- `diagnosticInfo`
- `diagnosticWarning`
- `diagnosticError`
- `muted`
- `panelAccent`

### Validation Rules

- Unknown role names emit diagnostics and are ignored.
- Missing role names use built-in defaults.
- The same role must render consistently wherever it appears.

## Color Value

User-provided color value.

### Accepted Forms

- Named terminal color.
- Hex RGB color.

### Validation Rules

- Invalid names or malformed hex values emit diagnostics and fall back for that role.
- If a role defines foreground/background colors and the pair is low contrast, the role emits a diagnostic and falls back to defaults.
- If a terminal cannot represent a valid color exactly, the dashboard may use the closest supported rendering while preserving readability.

## Preference Diagnostic

User-visible diagnostic produced while loading or applying preferences.

### Fields

- `severity`: warning or error.
- `message`: actionable explanation.
- `source`: optional preference file location.

### Validation Rules

- Invalid color values, unsupported layout values, unreadable preference files, unknown roles, and low-contrast pairs produce diagnostics.
- Diagnostics are visible in the dashboard and do not prevent startup.
