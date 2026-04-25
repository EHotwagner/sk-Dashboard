# Feature Specification: Dashboard Display Controls

**Feature Branch**: `003-dashboard-display-controls`  
**Created**: 2026-04-25  
**Status**: Draft  
**Input**: User description: "add a version number beside sk-daskboard. to all tables add altering background colors per row. add different hotkeys to make the current selected/active user story/featur/plan/task full screen with all available information."

## Clarifications

### Session 2026-04-25

- Q: Should full-screen views be modal detail views or allow item navigation while expanded? -> A: Modal detail view; each full-screen view displays exactly one target type at a time: feature, user story, plan, or task.
- Q: Should alternating row stripe colors be configurable or fixed? -> A: Add configurable stripe color roles with safe defaults.
- Q: What should "all available information" include in full-screen views? -> A: Parsed fields plus source artifact text for the selected target when available.

## Change Classification

- **Tier**: Tier 1 (contracted change)
- **Public API impact**: Extends public Core/Dashboard surface for dashboard version display, row stripe color roles, full-screen command identifiers, and modal full-screen state where required by `.fsi` contracts.
- **Compatibility impact**: Existing preferences and hotkeys remain valid. New stripe roles and full-screen commands are additive, with safe defaults when missing or invalid.
- **Verification approach**: Update `.fsi` signatures first, exercise the surface through FSI, add semantic tests before implementation, run dashboard rendering smoke checks, refresh public surface baselines, and run the evidence audit before merge readiness.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See Dashboard Version At A Glance (Priority: P1)

A dashboard user can see the installed dashboard version next to the dashboard name in the header, so they can confirm which build they are using when reporting issues, comparing behavior, or validating an update.

**Why this priority**: Version visibility helps users and maintainers diagnose behavior without leaving the dashboard.

**Independent Test**: Open the dashboard and confirm the header shows the dashboard name followed by the current version in a readable form.

**Acceptance Scenarios**:

1. **Given** the dashboard is opened, **When** the header is displayed, **Then** the dashboard name is followed by a visible version value.
2. **Given** the user has updated the dashboard tool, **When** they reopen the dashboard, **Then** the header version reflects the updated build.

---

### User Story 2 - Scan Tables With Alternating Rows (Priority: P2)

A dashboard user can scan all dashboard tables more easily because table rows alternate background colors consistently across feature, story, task, diagnostic, and detail-oriented tables, with stripe colors that can be configured as dashboard visual roles.

**Why this priority**: Alternating row backgrounds improve readability and reduce misreading when tables are dense or terminal panes are narrow.

**Independent Test**: Open the dashboard with multiple rows in each table and confirm adjacent rows use alternating backgrounds while selected, active, warning, and error states remain visually clear.

**Acceptance Scenarios**:

1. **Given** a dashboard table contains multiple rows, **When** the table is rendered, **Then** adjacent non-selected rows use alternating background colors.
2. **Given** a row is selected or active, **When** alternating row backgrounds are applied, **Then** the selected or active styling remains visually distinct from the stripe colors.
3. **Given** a table has one row or no rows, **When** it is rendered, **Then** the table remains readable and does not display stray stripe artifacts.
4. **Given** the user configures row stripe colors, **When** tables are rendered, **Then** the configured stripe colors are used where readable and safe defaults are used where configuration is missing or invalid.

---

### User Story 3 - Expand Current Dashboard Context Full Screen (Priority: P3)

A keyboard-only dashboard user can press distinct hotkeys to open a modal full-screen view for the current feature, user story, plan, or task, showing all available information for exactly that selected or active item without the constraints of the normal multi-pane layout.

**Why this priority**: Users often need to inspect the full text and context of the selected item without losing their place in the dashboard.

**Independent Test**: Navigate to a feature, user story, plan, and task, press each full-screen hotkey, verify the expanded view contains complete available information for only that requested item type, then exit back to the dashboard with the same selection preserved.

**Acceptance Scenarios**:

1. **Given** a feature is selected, **When** the user presses the feature full-screen hotkey, **Then** the dashboard shows an expanded feature view with all available parsed feature status, artifact, branch, source artifact text, and diagnostic information.
2. **Given** a user story is selected or active, **When** the user presses the story full-screen hotkey, **Then** the dashboard shows the story title, priority, description, acceptance scenarios, progress, related tasks, source location, available source artifact text, and available diagnostics.
3. **Given** a plan is loaded for the selected feature, **When** the user presses the plan full-screen hotkey, **Then** the dashboard shows all available parsed plan fields, raw source artifact text, and source context in a full-screen view.
4. **Given** a task is selected or active, **When** the user presses the task full-screen hotkey, **Then** the dashboard shows task status, title, description, dependencies, related story, source location, metadata, available source artifact text, and available diagnostic context.
5. **Given** the user is in any full-screen view, **When** they close that modal view, **Then** the dashboard returns to the previous layout with the same feature, story, and task selections preserved.
6. **Given** the user opens a feature, story, plan, or task full-screen view, **When** the view is displayed, **Then** it shows only the requested target type and does not combine all selected dashboard items into one screen.

### Edge Cases

- The dashboard version cannot be determined from the installed build metadata.
- A table contains selected, active, warning, or error rows that could conflict with alternating row backgrounds.
- The selected feature has missing or unreadable artifacts.
- No user story, plan, or task is currently available for the selected feature.
- The selected item has very long text, many related tasks, or many diagnostics.
- The selected item's source artifact text is missing or unreadable.
- A full-screen hotkey is pressed while another modal full-screen view is already open.
- A configured custom color theme makes stripe colors too similar to selected or active row colors.
- A configured row stripe color is invalid or too low contrast for readable table text.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The dashboard MUST display a version value beside the dashboard name in the primary header.
- **FR-002**: The version display MUST remain readable in both wide and narrow dashboard layouts.
- **FR-003**: If the version value is unavailable, the dashboard MUST show a clear fallback value rather than omitting the version area entirely.
- **FR-004**: All dashboard tables MUST use alternating background colors for adjacent non-selected data rows.
- **FR-005**: Alternating row backgrounds MUST apply consistently to feature rows, user story rows, task rows, diagnostic rows, and any table-like expanded views.
- **FR-006**: Selected, active, warning, and error row states MUST take precedence over alternating row backgrounds.
- **FR-007**: Alternating row backgrounds MUST remain compatible with configured dashboard color preferences and readable default colors.
- **FR-008**: Users MUST have distinct keyboard commands to open full-screen views for the current feature, user story, plan, and task.
- **FR-009**: Full-screen view commands MUST be reachable without a mouse and MUST be compatible with user-configurable hotkeys.
- **FR-010**: The feature full-screen view MUST show all available selected feature information, including branch, checkout state, artifact states, source locations where available, source artifact text when available, and diagnostics.
- **FR-011**: The user story full-screen view MUST show all available selected or active story information, including title, priority, description, acceptance scenarios, progress, related tasks, source location, source artifact text when available, and diagnostics.
- **FR-012**: The plan full-screen view MUST show all available plan information for the selected feature, including summary, technical context, checks, raw source artifact text, source location, and diagnostics when available.
- **FR-013**: The task full-screen view MUST show all available selected or active task information, including status, title, description, dependencies, related story, source location, metadata, source artifact text when available, and diagnostics.
- **FR-014**: Users MUST be able to exit a modal full-screen view and return to the dashboard without changing the selected feature, story, or task.
- **FR-015**: If a requested full-screen target is unavailable, the dashboard MUST show a readable message explaining what is missing and how the user can return.
- **FR-016**: Existing navigation, refresh, preference reload, and quit behavior MUST continue to work outside full-screen views.
- **FR-017**: A full-screen view MUST display exactly one requested target type at a time: feature, user story, plan, or task.
- **FR-018**: Row stripe colors MUST be exposed as configurable dashboard visual roles with safe default values.
- **FR-019**: Invalid or unreadable configured row stripe colors MUST be reported and replaced with safe default stripe colors.
- **FR-020**: Full-screen views MUST combine parsed fields with source artifact text when that source text is available for the selected target.

### Key Entities *(include if feature involves data)*

- **Dashboard Version Display**: The visible version label shown beside the dashboard name, including a fallback when the build version is unavailable.
- **Table Row Stripe**: Alternating row background state applied to table rows when no higher-priority row state is active; includes configurable visual roles and safe defaults.
- **Full-Screen Dashboard View**: A temporary modal focused view for one selected feature, user story, plan, or task that preserves the user's dashboard selection while showing parsed fields and available source artifact text for that one target type.
- **Full-Screen Command**: A keyboard command that opens one specific expanded view target and participates in the existing configurable hotkey model.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of dashboard launches show the dashboard name and a version value in the header.
- **SC-002**: Users can identify the dashboard version from the header in under 5 seconds during a manual review.
- **SC-003**: 100% of dashboard tables with two or more rows show alternating backgrounds on adjacent non-selected rows.
- **SC-004**: Selected or active rows remain visually distinguishable from striped rows in 100% of tested default and configured color scenarios.
- **SC-005**: Users can open the feature, story, plan, and task full-screen views using only the keyboard.
- **SC-006**: Users can return from any full-screen view to the normal dashboard with the same feature, story, and task selection preserved in 100% of navigation smoke checks.
- **SC-007**: Full-screen views show all available information for their target item without requiring horizontal side-by-side panes.
- **SC-008**: Existing keyboard navigation smoke checks continue to pass after the new full-screen commands are added.

## Assumptions

- "sk-daskboard" in the request refers to the dashboard header label currently shown as "sk-dashboard".
- The version should represent the installed dashboard application build, not the selected Speckit feature version.
- Alternating row backgrounds should be visual presentation only and should not change sorting, selection, or filtering.
- Full-screen views are temporary modal dashboard modes, not separate persisted user preferences.
- Distinct default hotkeys may be chosen during planning, and users may override them through the existing dashboard preferences file.
- When both "selected" and "active" context exist, the expanded view uses the selected item first and falls back to the active item only when no selected item is available.
- Row stripe colors should follow the existing dashboard preference model for configurable visual roles and validation diagnostics.
- Full-screen views do not discover unrelated repository files; they show source artifact text already associated with the selected feature, story, plan, or task.
