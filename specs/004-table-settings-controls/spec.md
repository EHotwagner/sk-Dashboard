# Feature Specification: Table Settings Controls

**Feature Branch**: `004-table-settings-controls`  
**Created**: 2026-04-25  
**Status**: Draft  
**Input**: User description: "make all tables scrollable, there can be too many items. add the possiblity for configurable borders in all tables. full detail mode also needs scrollable. also add some formatting and colors to the detail pages to make them more readable. add a comprehensive settings page that can be opened by hotkey. add an option to open the settings as a separate console app that can be opened on another screen and edit config. the dashboard needs to live check for changes in the config file so any changes can be seen live."

## Clarifications

### Session 2026-04-25

- Q: How should oversized tables coordinate vertical selection and horizontal scrolling? → A: Selection-anchored vertical scrolling, with explicit left/right horizontal scrolling and sticky identifying columns where available.
- Q: How should concurrent settings edits from the dashboard and separate console be handled? → A: Detect stale settings before save, show a conflict warning, and require the user to reload or explicitly overwrite.
- Q: How should live reload behave while a settings page has unsaved edits? → A: Defer external live reload for that settings surface and show a pending conflict notice.
- Q: How should the separate settings console be launched? → A: Add a settings mode to the existing `sk-dashboard` executable, opened with a CLI flag such as `sk-dashboard --settings`.
- Q: Which table border choices must be supported? → A: Support `none`, `minimal`, `rounded`, and `heavy` table border styles.

## Change Classification

**Tier**: Tier 1 (contracted change)

**Public API impact**: This feature adds or changes public Core surfaces for table viewport state, detail viewport state, display settings, table border preferences, color roles, settings edit sessions, live reload state, configuration diagnostics, and hotkey command identifiers.

**Compatibility impact**: Existing dashboard navigation, detail view, refresh, quit, hotkey preference loading, and configuration loading behavior must remain compatible. Unknown future configuration fields must not prevent known settings from loading.

**Verification approach**: Follow Spec -> FSI -> semantic tests -> implementation. Validate public `.fsi` additions through FSI, add semantic tests for Core parsing, reducers, and conflict behavior, add Dashboard smoke tests for rendered table, detail, and settings surfaces, and run the evidence graph and audit gates before merge readiness.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Navigate Large Tables Without Losing Context (Priority: P1)

A dashboard user can view and navigate every table even when it contains more rows or columns than fit on the screen, so dense feature, story, task, diagnostic, and detail data remains usable without truncating important information.

**Why this priority**: Large data sets are the immediate blocker. If users cannot reach table entries, the dashboard fails its primary browsing purpose.

**Independent Test**: Load dashboard data with more rows and longer values than the visible space can show, navigate each table, and confirm users can reach the hidden rows and columns while retaining enough context to understand what they are viewing.

**Acceptance Scenarios**:

1. **Given** a dashboard table contains more rows than the visible area, **When** the user navigates past the last visible row, **Then** the table scrolls to reveal additional rows without leaving the current dashboard screen.
2. **Given** a dashboard table contains values wider than the visible area, **When** the user uses table navigation for hidden content, **Then** the table reveals additional horizontal content while keeping the selected item understandable.
3. **Given** a table is scrolled away from its first row, **When** the user changes selection within that table, **Then** the visible region follows the selected row without unexpectedly jumping to unrelated content.
4. **Given** a full detail view contains more content than fits on screen, **When** the user navigates through the detail view, **Then** the hidden content becomes reachable without closing the detail view.

---

### User Story 2 - Configure Table Borders And Detail Readability (Priority: P2)

A dashboard user can adjust table borders and detail-page visual styling to match their terminal, preference, and readability needs, making dense dashboard information easier to scan.

**Why this priority**: Once all content is reachable, users need readable presentation controls that work across different terminals and personal accessibility needs.

**Independent Test**: Change table border settings and detail-page color/formatting settings, then confirm every table and detail page reflects the chosen presentation while remaining readable.

**Acceptance Scenarios**:

1. **Given** the user selects a table border style, **When** any dashboard table is shown, **Then** that table uses the selected border style consistently.
2. **Given** the user disables or minimizes table borders, **When** tables are shown, **Then** the table content remains aligned and readable.
3. **Given** the user changes detail-page formatting or color preferences, **When** a detail page is opened, **Then** headings, labels, status values, and body text are visually separated enough to scan.
4. **Given** a configured color or border option is invalid or unreadable, **When** the dashboard applies settings, **Then** it keeps the page usable with safe defaults and reports the configuration problem clearly.

---

### User Story 3 - Edit Settings From The Dashboard Or Another Screen (Priority: P3)

A dashboard user can open a comprehensive settings experience by hotkey inside the dashboard, or open a separate settings console on another screen, edit configuration there, and see the running dashboard update after the configuration changes.

**Why this priority**: Configuration becomes much more useful when users can discover settings from the UI, edit them without leaving the workflow, and preview changes live.

**Independent Test**: Open settings from the dashboard with a hotkey, change display options, verify the dashboard updates, then open the separate settings console, change the same options, and verify the running dashboard reflects the saved changes.

**Acceptance Scenarios**:

1. **Given** the dashboard is running, **When** the user presses the settings hotkey, **Then** a comprehensive settings page opens without losing the current dashboard selection.
2. **Given** the settings page is open, **When** the user reviews available settings, **Then** table scrolling, table borders, detail formatting, detail colors, hotkeys, and live configuration behavior are discoverable and editable.
3. **Given** the user saves a settings change from the dashboard settings page, **When** they return to the dashboard, **Then** the visible dashboard reflects the saved setting without requiring a restart.
4. **Given** the running dashboard is on one screen and the separate settings console is opened on another, **When** the user saves configuration changes in the separate settings console, **Then** the running dashboard detects and applies the updated configuration.
5. **Given** the configuration file changes while the dashboard is running, **When** the change is valid, **Then** the dashboard updates the affected visible settings automatically within a short period.
6. **Given** the configuration file changes while the dashboard is running, **When** the change is invalid or incomplete, **Then** the dashboard keeps the last usable settings, reports the problem, and continues running.

### Edge Cases

- A table has thousands of rows, very long text values, no rows, or only one row.
- The selected row is deleted, hidden, or moved after data refresh while the table is scrolled.
- The terminal is resized while a table or full detail view is scrolled.
- Multiple tables are visible at once and the user switches focus between them.
- Full detail mode contains long source text, many related items, or many diagnostics.
- Border settings make separators too heavy, too sparse, or unsupported by the user's terminal.
- Detail-page colors conflict with warning, error, selected, or active status colors.
- The settings hotkey conflicts with an existing command or user-configured binding.
- The separate settings console and running dashboard save or read the configuration around the same time.
- The configuration file is missing, partially written, unreadable, or contains unknown settings.
- Live configuration reload occurs while a settings page or detail page is open.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every dashboard table MUST support vertical scrolling when its row count exceeds the visible area.
- **FR-002**: Every dashboard table with content wider than the visible area MUST provide explicit keyboard commands for horizontal scrolling.
- **FR-003**: Table scrolling MUST keep the current selected or focused row visible whenever that row exists.
- **FR-004**: Table scrolling MUST preserve enough row and column context for users to understand the visible content while navigating, including sticky identifying columns where a table has stable identifier columns.
- **FR-005**: Full detail mode MUST support scrolling for all content that exceeds the visible area.
- **FR-006**: Full detail mode scrolling MUST preserve the user's selected feature, story, plan, task, or diagnostic context when opened and closed.
- **FR-007**: Users MUST be able to configure table border presentation for all dashboard tables with supported styles `none`, `minimal`, `rounded`, and `heavy`.
- **FR-008**: Table border settings MUST apply consistently to feature, story, task, diagnostic, settings, and detail-oriented tables.
- **FR-009**: If a chosen table border style is unsupported or invalid, the dashboard MUST use a readable fallback and notify the user through configuration feedback.
- **FR-010**: Detail pages MUST use formatting that clearly distinguishes headings, labels, status values, metadata, body text, warnings, errors, and source text.
- **FR-011**: Users MUST be able to configure detail-page colors for key readable roles, including headings, labels, normal text, muted text, success, warning, error, and selected or active context.
- **FR-012**: Invalid or low-readability color settings MUST be rejected or replaced with safe defaults while preserving dashboard operation.
  For this feature, a color setting is considered invalid or low-readability when Spectre.Console cannot parse it, when foreground and background resolve to the same named color, or when a configured selected/active foreground-background pair is one of the project-defined rejected pairs in Core validation tests. Unknown color roles are ignored without blocking known roles.
- **FR-013**: The dashboard MUST provide a comprehensive settings page that can be opened by a keyboard hotkey.
- **FR-014**: The settings page MUST allow users to view and edit dashboard display settings, including table scrolling behavior, table borders, detail formatting, detail colors, hotkeys, and live configuration reload behavior.
- **FR-015**: Opening and closing the settings page MUST preserve the user's current dashboard selection and navigation context.
- **FR-016**: Settings changes saved from the dashboard settings page MUST be reflected in the running dashboard without requiring the user to restart it.
- **FR-017**: Users MUST have an option to open settings as a separate console experience suitable for use on another screen by launching the existing `sk-dashboard` executable in settings mode with a CLI flag such as `sk-dashboard --settings`.
- **FR-018**: The separate settings console MUST read from and write to the same dashboard configuration as the main dashboard.
- **FR-019**: Settings changes saved from the separate settings console MUST become visible in the running dashboard without requiring a dashboard restart.
- **FR-020**: The running dashboard MUST detect changes to the configuration file while it is open.
- **FR-021**: When the configuration file changes to a valid state, the dashboard MUST apply changed display and hotkey preferences automatically within 2 seconds under normal local file conditions.
- **FR-022**: When the configuration file changes to an invalid or unreadable state, the dashboard MUST continue running with the last valid settings and show clear feedback about the configuration problem.
- **FR-023**: Live configuration reload MUST avoid interrupting active table navigation, full detail scrolling, or settings edits.
- **FR-024**: Unknown future configuration fields MUST not prevent known valid settings from being applied.
- **FR-025**: The settings experience MUST expose current values, editable choices, validation feedback, and a clear way to save or discard changes.
- **FR-026**: When a settings surface attempts to save after the configuration file has changed since it was loaded, it MUST show a conflict warning and require the user to reload current settings or explicitly overwrite them before writing.
- **FR-027**: While a settings surface has unsaved edits, external configuration reloads MUST be deferred for that settings surface and shown as a pending conflict notice until the user saves, discards, reloads, or explicitly overwrites.

### Key Entities *(include if feature involves data)*

- **Scrollable Table View**: A dashboard table with visible window position, selected row, horizontal position, and contextual labels that help users navigate oversized data.
- **Detail Page Presentation**: Formatting and color roles used to make expanded detail content readable, including headings, metadata, status values, body text, diagnostics, and source text.
- **Table Border Preference**: A configurable presentation choice with supported values `none`, `minimal`, `rounded`, and `heavy` that controls table border style consistently across all dashboard table surfaces.
- **Dashboard Settings**: User-editable preferences for table behavior, borders, detail formatting, colors, hotkeys, and live reload behavior.
- **Settings Page**: The in-dashboard settings surface opened by hotkey, used to inspect, validate, save, and discard dashboard settings.
- **Separate Settings Console**: A standalone settings experience that edits the same dashboard configuration while the dashboard continues running elsewhere.
- **Configuration File State**: The saved dashboard configuration plus its validity, last usable version, validation messages, and live reload status.
- **Settings Edit Session**: A dashboard or separate-console editing session with loaded settings, unsaved changes, the configuration version or timestamp observed at load time, and conflict status before save.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can reach the final row in every dashboard table containing 500 rows using only keyboard navigation.
- **SC-002**: Users can inspect hidden horizontal content in every dashboard table containing values wider than the visible area.
- **SC-003**: Users can scroll from the start to the end of a full detail view containing at least 2,000 lines without closing the detail view.
- **SC-004**: 100% of visible dashboard table surfaces reflect the selected table border preference after settings are applied.
- **SC-005**: 90% of reviewed detail pages are rated readable by maintainers using the default formatting and color settings.
- **SC-006**: Users can open the settings page from the dashboard by hotkey in under 2 seconds during normal operation.
- **SC-007**: Users can locate and edit table borders, detail colors, and hotkey settings from the settings page in under 60 seconds during a usability review.
- **SC-008**: Valid settings changes saved from either settings surface are visible in the running dashboard within 2 seconds under normal local file conditions.
- **SC-009**: Invalid configuration changes never crash the running dashboard during validation smoke checks.
- **SC-010**: Existing dashboard navigation, detail view, refresh, and quit workflows continue to pass their smoke checks after live configuration reload is enabled.

## Assumptions

- "All tables" includes feature, user story, task, diagnostic, settings, and detail-oriented table surfaces shown by the dashboard.
- "Full detail mode" refers to the dashboard's expanded detail view for the currently selected or active item.
- The separate settings console is a user-facing companion settings experience, not a second dashboard instance.
- Settings edited in either settings surface are stored in the dashboard's existing configuration location.
- The dashboard should favor the last valid configuration over partial or invalid changes so active work is not interrupted.
- Live reload applies to settings that can safely affect the running UI; settings that require restart must be clearly identified if any are introduced later.
- Default border, color, and formatting choices should be readable in common dark and light terminal themes.
