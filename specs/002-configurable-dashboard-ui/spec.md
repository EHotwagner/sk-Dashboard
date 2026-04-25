# Feature Specification: Configurable Dashboard UI

**Feature Branch**: `002-configurable-dashboard-ui`  
**Created**: 2026-04-25  
**Status**: Draft  
**Input**: User description: "make the colors configurable and add layoutoptions for widescreen vs vertical"

## Clarifications

### Session 2026-04-25

- Q: Should layout selection be manual, automatic, or both? -> A: Manual plus automatic default: users may choose `widescreen`, `vertical`, or `auto`; default is `auto`.
- Q: Should UI settings share the same preference file as hotkeys? -> A: Single preferences file containing both hotkeys and UI settings.
- Q: Which color value formats should user preferences accept? -> A: Named terminal colors and hex RGB colors.
- Q: How should low-contrast color combinations be handled? -> A: Warn and fall back to defaults for low-contrast foreground/background pairs.
- Q: What terminal width threshold should automatic layout use? -> A: Use vertical below 120 columns; widescreen at 120+ columns.

## Change Classification

- **Tier**: Tier 1 contracted change.
- **Public API Impact**: Updates Core public preference-loading surface exposed through `.fsi` signatures for dashboard preferences, UI preferences, color roles, layout modes, and validation diagnostics.
- **Compatibility Impact**: Existing hotkey preference files remain valid. UI preferences are optional; missing UI settings use defaults.
- **Verification Approach**: Draft updated `.fsi` signatures first, exercise the preference-loading surface through F# Interactive, add semantic tests for parsing, validation, fallback, layout selection, and reload behavior, then implement.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Customize Dashboard Colors (Priority: P1)

A dashboard user can choose the colors used for key visual roles, such as selected items, last activity, progress, warnings, errors, muted text, and panel accents, so the dashboard matches their terminal theme and remains readable.

**Why this priority**: Color choices directly affect readability and accessibility. Users with dark, light, low-contrast, or personalized terminal themes need control before more layout options are valuable.

**Independent Test**: Can be tested by providing custom color preferences, opening the dashboard, and confirming that the configured colors appear consistently across features, stories, tasks, diagnostics, progress indicators, and activity highlights.

**Acceptance Scenarios**:

1. **Given** a user has configured custom colors for selected items and activity markers using named terminal colors or hex RGB colors, **When** the dashboard opens, **Then** selected rows and last-activity rows use the configured colors.
2. **Given** a user has configured diagnostic colors, **When** diagnostics are displayed, **Then** informational, warning, and error diagnostics use the configured colors.
3. **Given** no custom colors are configured, **When** the dashboard opens, **Then** it uses the built-in color set without prompting or failing.
4. **Given** a configured foreground/background color pair has low contrast, **When** the dashboard opens, **Then** the dashboard reports the low-contrast pair and uses the default colors for that visual role.

---

### User Story 2 - Choose Widescreen or Vertical Layout (Priority: P2)

A dashboard user can choose between a widescreen layout optimized for broad terminals, a vertical layout optimized for narrow or stacked terminals, and an automatic layout option that selects the best arrangement for the current terminal shape.

**Why this priority**: Users run the dashboard in different terminal shapes, including wide desktop terminals and narrow editor panes. A layout option makes the same information usable in both contexts.

**Independent Test**: Can be tested by selecting each layout option, opening the dashboard at matching terminal sizes, and confirming that the information order, navigation, and selected-item visibility remain usable.

**Acceptance Scenarios**:

1. **Given** the user selects the widescreen layout, **When** the dashboard opens in a wide terminal, **Then** feature/story context and plan/task details are displayed side by side.
2. **Given** the user selects the vertical layout, **When** the dashboard opens in a narrow terminal, **Then** dashboard sections are stacked in a readable order without relying on side-by-side panes.
3. **Given** the user selects automatic layout, **When** the dashboard opens or the terminal shape changes, **Then** the dashboard uses vertical layout below 120 columns and widescreen layout at 120 or more columns.
4. **Given** the user changes the layout option and refreshes or restarts the dashboard, **When** the dashboard is shown again, **Then** the chosen layout is used.

---

### User Story 3 - Recover from Invalid UI Preferences (Priority: P3)

A dashboard user receives clear feedback when color or layout preferences are invalid, while the dashboard remains usable with safe defaults.

**Why this priority**: Preference files are easy to mistype. Invalid settings must not prevent users from opening the dashboard or reviewing project status.

**Independent Test**: Can be tested by providing invalid color names, unsupported layout names, and partial preference data, then confirming the dashboard displays diagnostics and falls back only for invalid values.

**Acceptance Scenarios**:

1. **Given** a configured color value is invalid, **When** the dashboard opens, **Then** the dashboard reports the invalid value and uses the default color for that role.
2. **Given** a configured layout value is unsupported, **When** the dashboard opens, **Then** the dashboard reports the unsupported layout and uses the default layout.
3. **Given** some UI preferences are valid and others are invalid, **When** the dashboard opens, **Then** valid preferences are applied and invalid preferences are reported individually.

### Edge Cases

- Terminal does not support one or more configured named or hex colors.
- Configured colors produce low contrast between foreground and background.
- The configured layout is widescreen but the terminal is too narrow.
- The configured layout is vertical but the terminal is very wide.
- Preference file is missing, empty, unreadable, or only partially valid.
- User changes UI preferences while the dashboard is running and reloads settings.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to configure colors for dashboard visual roles, including selected item, last activity, progress complete, progress incomplete, diagnostic info, diagnostic warning, diagnostic error, muted text, and panel accent.
- **FR-002**: Users MUST be able to express each configurable color as either a named terminal color or a hex RGB color.
- **FR-017**: Configurable color roles MUST support either a single color value or, where the visual role uses both text and background styling, a foreground/background color pair.
- **FR-003**: The dashboard MUST use built-in colors for every visual role when no custom color is configured.
- **FR-014**: The dashboard MUST apply configured colors consistently wherever the same visual role appears.
- **FR-004**: The dashboard MUST allow users to select a layout mode from `widescreen`, `vertical`, and `auto`.
- **FR-005**: The widescreen layout MUST prioritize side-by-side comparison of navigation context and detail context.
- **FR-006**: The vertical layout MUST prioritize top-to-bottom readability in narrow or stacked terminal spaces.
- **FR-007**: The dashboard MUST preserve existing keyboard navigation behavior across both layout modes.
- **FR-008**: The dashboard MUST preserve selected feature, story, and task context when changing visual preferences or layout options.
- **FR-009**: The dashboard MUST report invalid color values and unsupported layout values as visible diagnostics without preventing dashboard startup.
- **FR-010**: The dashboard MUST apply valid preferences even when other preferences in the same configuration are invalid.
- **FR-011**: Users MUST be able to reload UI preferences during a dashboard session without restarting.
- **FR-012**: User interface preferences and hotkey preferences MUST be configurable from a single dashboard preferences file.
- **FR-013**: Documentation MUST describe the supported visual roles, layout modes, default behavior, and examples of valid configuration.
- **FR-015**: The dashboard MUST report low-contrast foreground/background color pairs and use default colors for the affected visual role.
- **FR-016**: Automatic layout MUST use vertical layout below 120 terminal columns and widescreen layout at 120 or more terminal columns.

### Key Entities *(include if feature involves data)*

- **Dashboard Preference Set**: User-owned configuration for the dashboard. Includes hotkey preferences, color role preferences, selected layout mode, and validation diagnostics.
- **UI Preference Set**: The visual subset of the dashboard preference set. Includes color role preferences and selected layout mode.
- **Color Role**: A named visual purpose such as selected item, last activity, progress, diagnostic severity, muted text, or panel accent. Each role maps to a color value.
- **Color Value**: A user-provided color expressed as a named terminal color or a hex RGB color.
- **Layout Mode**: A named dashboard arrangement optimized for a terminal shape. Includes widescreen, vertical, and automatic modes.
- **Preference Diagnostic**: A user-visible message explaining invalid, unsupported, unreadable, or ignored UI preference values.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can configure at least 9 distinct visual color roles and see them reflected in the dashboard after startup or reload.
- **SC-002**: Users can switch between widescreen, vertical, and automatic layouts in under 30 seconds using documented preferences.
- **SC-003**: Below 120 terminal columns, automatic layout keeps all primary sections readable without horizontal truncation of section headers.
- **SC-004**: At 120 or more terminal columns, automatic layout displays navigation context and detail context simultaneously.
- **SC-005**: Invalid UI preference files never prevent the dashboard from opening; users receive visible diagnostics for every invalid setting.
- **SC-006**: Existing keyboard navigation scenarios continue to pass in both layout modes.
- **SC-007**: Low-contrast foreground/background pairs are reported and replaced with readable defaults for 100% of affected visual roles.

## Assumptions

- UI preferences and hotkeys are stored in a single dashboard preferences file.
- The default layout is automatic for users who do not configure layout options.
- The vertical layout is intended for constrained terminal widths and editor-integrated panes, not for touch or mouse-driven usage.
- Color configurability covers dashboard presentation roles, not arbitrary per-task or per-story custom coloring.
- When a terminal cannot represent a configured named or hex color exactly, the dashboard may use the closest supported rendering while preserving readability.
- Readability takes precedence over exact user color preference when a configured foreground/background pair is too low contrast to use safely.
