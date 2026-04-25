# Data Model: Dashboard Display Controls

## Dashboard Version Display

Visible version label shown next to `sk-dashboard` in the primary header.

### Fields

- `label`: rendered version text, including a leading `v` or equivalent readable prefix.
- `source`: metadata source used to resolve the version, such as package metadata, assembly metadata, or fallback.
- `diagnostic`: optional warning when version metadata is unavailable.

### Validation Rules

- Missing or empty metadata uses a clear fallback value instead of hiding the version area.
- The label must fit in both wide and narrow layouts.
- The version represents the dashboard application build, not the selected Speckit feature.

## Table Row Stripe

Alternating background style applied to table-like dashboard rows when no higher-priority row state is active.

### Fields

- `index`: zero-based visible data row index within the current table.
- `role`: `rowStripeOdd` or `rowStripeEven`.
- `style`: resolved foreground/background style from dashboard UI preferences.
- `appliesTo`: feature, user story, task, diagnostic, or detail table row.

### Validation Rules

- Header rows and empty table placeholders are not striped.
- Selected, active, warning, and error row states override stripe styles.
- Tables with zero or one data row remain readable and do not render stray stripe artifacts.
- Invalid, missing, or low-contrast stripe preferences produce diagnostics and fall back to safe defaults.

## Dashboard UI Preference Set

Existing user-owned dashboard configuration extended with row stripe roles.

### Fields

- `bindings`: existing hotkey binding list.
- `ui.layout`: existing layout mode.
- `ui.colors.rowStripeOdd`: configurable stripe style for odd visible data rows.
- `ui.colors.rowStripeEven`: configurable stripe style for even visible data rows.

### Validation Rules

- Missing stripe roles use built-in defaults.
- Invalid color values affect only the invalid role.
- Low-contrast foreground/background pairs emit diagnostics and fall back for the affected stripe role.
- Preference reload applies valid stripe and hotkey changes without resetting selected feature, story, or task.

## Full-Screen Dashboard View

Temporary modal view focused on one selected target type.

### Fields

- `target`: one of `feature`, `story`, `plan`, or `task`.
- `selectedFeatureId`: feature selection preserved while modal is open.
- `selectedStoryId`: story selection preserved while modal is open.
- `selectedTaskId`: task selection preserved while modal is open.
- `parsedFields`: dashboard fields available for the requested target.
- `sourceLocation`: source path and optional line for the requested target when known.
- `sourceText`: raw associated source artifact text when available.
- `diagnostics`: target-specific diagnostics or missing-target message.

### Validation Rules

- A full-screen view displays exactly one target type at a time.
- Opening a full-screen view does not change feature, story, or task selections.
- Closing the view clears only modal state and returns to the previous dashboard layout.
- Missing target data shows a readable message with return guidance.
- Long text wraps or scrolls without requiring side-by-side panes.

## Full-Screen Command

Keyboard command that opens one specific modal target.

### Fields

- `command`: one of `fullscreen.feature`, `fullscreen.story`, `fullscreen.plan`, or `fullscreen.task`.
- `keySequence`: default or user-configured key sequence.
- `scope`: dashboard command scope.
- `source`: default or preference file source.

### Validation Rules

- Commands must be reachable without a mouse.
- Commands participate in the existing hotkey validation and diagnostics path.
- Conflicting or invalid bindings are reported consistently with existing hotkey diagnostics.
- Closing uses the existing close/escape behavior or a documented close command without changing selections.

## State Transitions

- Normal dashboard -> feature full-screen: feature target opens for selected feature.
- Normal dashboard -> story full-screen: story target opens for selected story, or active story fallback when no selected story exists.
- Normal dashboard -> plan full-screen: plan target opens for selected feature's loaded plan.
- Normal dashboard -> task full-screen: task target opens for selected task, or active task fallback when no selected task exists.
- Any full-screen target -> normal dashboard: modal closes and prior selections remain.
- Full-screen target -> different full-screen target: replaces modal target with the newly requested target without combining content.
