# Data Model: Table Settings Controls

## Scrollable Table View

Dashboard table viewport state for a feature, story, task, diagnostic, settings, or detail-oriented table.

Fields:

- `surfaceId`: stable table id such as `features`, `stories`, `tasks`, `diagnostics`, or `settings`.
- `selectedRowId`: selected row id when the table has selectable rows.
- `verticalOffset`: zero-based first visible data row.
- `horizontalOffset`: zero-based scroll position for non-sticky columns/content.
- `visibleRowCapacity`: current renderable row count after terminal layout.
- `stickyColumns`: identifying columns that remain visible when horizontally scrolled.

Validation:

- Offsets are clamped to available rows/columns after refresh or resize.
- If the selected row exists, `verticalOffset` is adjusted so it remains visible.
- Empty and one-row tables preserve readable placeholder rows and do not create invalid offsets.

## Detail Page Presentation

Display settings and viewport state for expanded detail content.

Fields:

- `target`: feature, story, plan, task, diagnostic, or settings detail target.
- `selectedContext`: selected feature/story/task ids captured when opened.
- `lineOffset`: zero-based first visible detail line.
- `horizontalOffset`: horizontal position for long source text or wide metadata.
- `formattingEnabled`: whether enhanced headings, labels, and metadata formatting are active.
- `colorRoles`: role-to-style map for headings, labels, normal text, muted text, success, warning, error, and selected/active context.

Validation:

- Detail offsets are clamped when content length or terminal size changes.
- Closing detail mode restores the captured dashboard selection.
- Invalid or low-readability color roles fall back to safe defaults with diagnostics.

## Table Border Preference

Global dashboard table border style.

Fields:

- `style`: one of `none`, `minimal`, `rounded`, or `heavy`.
- `source`: default, config path, dashboard settings page, or standalone settings console.
- `diagnostic`: optional validation diagnostic for fallback behavior.

Validation:

- Unknown values are rejected and replaced with the default readable border style.
- The chosen style applies to every dashboard table surface.

## Dashboard Settings

User-editable dashboard configuration persisted as JSON.

Fields:

- `version`: configuration schema version.
- `bindings`: hotkey command bindings.
- `ui.layout`: auto, widescreen, or vertical.
- `ui.table.borderStyle`: table border preference.
- `ui.table.horizontalScrollStep`: number of columns/cells moved by explicit horizontal scroll commands.
- `ui.detail.formattingEnabled`: detail formatting toggle.
- `ui.colors`: color role settings for dashboard and detail readability roles.
- `liveReload.enabled`: whether the dashboard watches the config file.
- `liveReload.debounceMilliseconds`: minimum delay before applying a file change.

Validation:

- Unknown future fields are ignored and preserved where practical.
- Known invalid values produce diagnostics and fall back by field.
- Valid known settings are applied even when unknown fields exist.

## Settings Page

In-dashboard settings surface opened by hotkey.

Fields:

- `openedFromSelection`: selected feature/story/task ids at open time.
- `editSession`: current settings edit session.
- `activeSection`: table behavior, borders, detail formatting/colors, hotkeys, or live reload.
- `validationMessages`: current field-level validation messages.

State transitions:

- Open by settings hotkey while preserving dashboard context.
- Save valid changes and apply them to the running dashboard.
- Discard changes and return to the previous dashboard context.
- Reload or explicitly overwrite after stale-save conflict.

## Separate Settings Console

Settings-only mode launched with `sk-dashboard --settings`.

Fields:

- `configPath`: shared dashboard config file path.
- `editSession`: current settings edit session.
- `validationMessages`: current field-level validation messages.

State transitions:

- Load current config and show editable settings.
- Save valid changes to the shared config.
- Detect stale settings before save and require reload or explicit overwrite.
- Exit without affecting a running dashboard when changes are discarded.

## Configuration File State

Runtime state for the shared config file and last valid settings.

Fields:

- `path`: resolved config path.
- `lastObservedVersion`: timestamp, size, or content hash observed at last successful load.
- `lastValidSettings`: parsed settings currently applied to the dashboard.
- `pendingDiagnostics`: validation or IO diagnostics from the most recent read.
- `reloadStatus`: idle, pending, applied, failed, or deferred.

Validation:

- Missing config uses defaults without crashing.
- Partial, unreadable, or invalid config keeps `lastValidSettings`.
- Valid file changes apply within 2 seconds under normal local file conditions.

## Settings Edit Session

Versioned edit state used by both settings surfaces.

Fields:

- `loadedSettings`: settings read when the session opened.
- `draftSettings`: unsaved edits.
- `loadedFileVersion`: file timestamp/size/hash at session open.
- `isDirty`: whether draft differs from loaded settings.
- `conflictStatus`: none, pendingExternalChange, staleBeforeSave, overwriteConfirmed.

Validation:

- Saving compares `loadedFileVersion` with the current file version.
- Stale saves show a conflict warning and require reload or explicit overwrite.
- External live reload is deferred for dirty settings surfaces and shown as a pending conflict notice.
