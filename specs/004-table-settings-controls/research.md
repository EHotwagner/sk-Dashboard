# Research: Table Settings Controls

## Decision: Keep the existing preference file as the shared dashboard configuration

**Rationale**: `SpeckitArtifacts.resolveUserConfigPath ()` and `Hotkeys.loadPreferences` already define a user-owned config path, JSON parser, diagnostics, and safe defaults. Extending that file keeps dashboard and `sk-dashboard --settings` in one contract and avoids a migration to a second store.

**Alternatives considered**: A separate settings file was rejected because the spec requires both settings surfaces to edit the same dashboard configuration. Environment variables were rejected because they do not support comprehensive editable settings or conflict detection.

## Decision: Model display settings as typed Core values and expose them through `.fsi`

**Rationale**: Border styles, detail color roles, live reload behavior, and hotkey bindings affect public dashboard state and semantic tests. Putting the contract in Core keeps parsing, validation, defaults, and compatibility behavior testable without depending on Spectre.Console renderables.

**Alternatives considered**: Keeping settings as raw JSON in Dashboard was rejected because it would duplicate validation and make FSI-first testing awkward. Adding a new settings library was rejected because the project already has a small JSON preference parser and no new dependency is needed.

## Decision: Implement selection-anchored viewport state for tables and detail pages

**Rationale**: The spec requires vertical scrolling to keep the focused row visible and full detail content reachable without closing the view. Storing each table/detail viewport as first visible row/line plus horizontal offset lets the reducer keep selection visible after navigation, refresh, and resize while preserving context.

**Alternatives considered**: Rendering all rows and relying on terminal clipping was rejected because hidden rows would remain unreachable. Page-only scrolling was rejected because selection changes must scroll automatically and predictably.

## Decision: Use explicit horizontal scroll commands plus sticky identifying columns

**Rationale**: Wide terminal tables need intentional left/right movement. Sticky identifier columns keep feature/story/task ids or severity/context visible while non-identifying columns scroll horizontally, matching the clarification and preserving row meaning.

**Alternatives considered**: Automatic horizontal scrolling on every selection change was rejected because it can move content unexpectedly. Wrapping every cell was rejected because it destroys dense table scanning and can create huge rows.

## Decision: Map border preferences to Spectre.Console table border variants at render time

**Rationale**: Spectre.Console already owns terminal table drawing. A small mapping from `none`, `minimal`, `rounded`, and `heavy` to supported `TableBorder` styles keeps the preference global and applies consistently across feature, story, task, diagnostic, settings, and detail-oriented tables.

**Alternatives considered**: Hand-drawing table borders was rejected because it would duplicate Spectre.Console behavior and increase terminal compatibility risk. Per-table border settings were rejected because the requirement is a global consistent presentation.

## Decision: Treat settings edits as versioned edit sessions with stale-save detection

**Rationale**: Both the in-dashboard settings page and standalone settings console can save to the same file. Capturing the config file version/timestamp at load time lets saves detect external changes, show a conflict warning, and require reload or explicit overwrite.

**Alternatives considered**: Last-writer-wins was rejected by clarification. File locking was rejected because a terminal settings workflow should not block the dashboard from reading the last valid config.

## Decision: Live reload through debounced file observation with last-valid settings retention

**Rationale**: A `FileSystemWatcher` or timestamp polling fallback can detect local file changes quickly, then reload through the same parser. Debouncing avoids partial-write reads. If parsing fails, the running dashboard keeps the last valid settings and surfaces diagnostics.

**Alternatives considered**: Manual reload only was rejected because FR-020 and FR-021 require live detection. Restart-required settings were rejected for the display and hotkey preferences in scope; future restart-required options must be called out explicitly.

## Decision: Add `sk-dashboard --settings` as a settings-only console mode

**Rationale**: Reusing the existing executable matches the clarification, avoids another package/tool, and lets settings mode share argument parsing, config path selection, validation, and save logic.

**Alternatives considered**: A second executable was rejected by clarification. Opening an external editor was rejected because the feature requires a comprehensive settings console, validation feedback, and conflict handling.
