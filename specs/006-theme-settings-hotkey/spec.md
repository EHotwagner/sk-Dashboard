# Feature Specification: Theme Settings and Checklist Hotkey

**Feature Branch**: `006-theme-settings-hotkey`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "create themes that are a bundle of settings with a display name. i want 2 different kind of settings that have their own themes. the basic tui app with table stettings, fontcolors, backgroundcolor, borders.... make it comprehensive and add a standard theme that honors the os or app setting and is either light or dark. light, dark are also manually selectable. so its def/light/dark and whatever custom themes there are in the themes folder. alternate row shading is off in by default. the other separate kind of settings with their own themes is the markdown rendering. it should honor the basic app light/dark settings i want different colors for different kind of elements(headlines/cursive/..) i want space/nelines between sections/paragraphs... to make it more readable. add a plain theme that is the current one and a def one that you are creating. themes must be selectable from the in app settings. also add a hotkey for viewing speckit checklists."

## Change Classification

**Tier**: Tier 1 (contracted change)

**Reason**: This changes user-visible dashboard presentation, settings behavior, theme discovery, Markdown rendering choices, and hotkey command behavior.

**Public API Impact**: Expected if dashboard settings, theme definitions, Markdown presentation settings, hotkey identifiers, checklist view state, or validation diagnostics are exposed through public `.fsi` surfaces.

**Verification Approach**: Validate through semantic tests for theme selection, theme discovery, light/dark resolution, settings persistence, hotkey contracts, and checklist view state; add dashboard smoke tests for app themes, Markdown themes, settings selection, custom theme loading, and checklist viewing.

## Clarifications

### Session 2026-04-26

- Q: What is the primary goal of Markdown rendering themes? → A: Make Markdown content more easily consumable for humans.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Select App Theme Bundles (Priority: P1)

A dashboard user can choose a named app theme that bundles the main terminal UI presentation settings, so the dashboard can consistently adapt table display, colors, backgrounds, borders, and related visual settings without changing each option one by one.

**Why this priority**: App theme selection is the foundation for the requested light, dark, default, and custom presentation behavior.

**Independent Test**: Open in-app settings, select each built-in app theme, and confirm dashboard tables, text colors, background colors, borders, selection state, status colors, and alternate row shading follow the selected theme.

**Acceptance Scenarios**:

1. **Given** the user opens in-app settings, **When** they view app theme choices, **Then** `default`, `light`, `dark`, and discovered custom app themes are shown with display names.
2. **Given** the user selects the `light` app theme, **When** they return to dashboard views, **Then** the TUI uses the light app theme's colors, backgrounds, table settings, and borders.
3. **Given** the user selects the `dark` app theme, **When** they return to dashboard views, **Then** the TUI uses the dark app theme's colors, backgrounds, table settings, and borders.
4. **Given** the user selects the `default` app theme, **When** the app or operating environment indicates light or dark preference, **Then** the dashboard resolves the app theme to the matching light or dark presentation.
5. **Given** alternate row shading has not been explicitly enabled, **When** any app theme is applied, **Then** alternate row shading remains off by default.

---

### User Story 2 - Select Markdown Rendering Themes (Priority: P2)

A dashboard user can choose a separate named Markdown rendering theme that controls document readability settings, so Markdown content is easier for humans to consume while still coordinating with the app's light or dark mode.

**Why this priority**: Markdown document readability has different settings from the surrounding TUI and should be customizable without forcing a full app theme change.

**Independent Test**: Open in-app settings, select `plain`, `default`, and custom Markdown themes, then open Markdown-backed document views and confirm headings, emphasis, paragraphs, sections, lists, code, links, and spacing make the content easier to scan, understand, and navigate.

**Acceptance Scenarios**:

1. **Given** the user opens in-app settings, **When** they view Markdown theme choices, **Then** `plain`, `default`, and discovered custom Markdown themes are shown with display names.
2. **Given** the user selects the `plain` Markdown theme, **When** a Markdown-backed view is opened, **Then** it matches the current Markdown presentation behavior as closely as possible.
3. **Given** the user selects the `default` Markdown theme, **When** a Markdown-backed view is opened, **Then** headings, emphasized text, links, code, block quotes, lists, and normal text use readable themed colors and spacing that improve human comprehension.
4. **Given** the app theme resolves to light or dark mode, **When** the Markdown theme is applied, **Then** the Markdown colors and contrast honor that resolved mode unless the selected Markdown theme explicitly defines its own compatible values.
5. **Given** a Markdown document contains multiple sections and paragraphs, **When** it is rendered with a readable Markdown theme, **Then** section and paragraph spacing makes the document easier to scan than the plain baseline.

---

### User Story 3 - Use Custom Themes From Theme Folders (Priority: P3)

A dashboard user can add custom app and Markdown themes to theme folders and select them from in-app settings, so teams can share preferred presentation bundles without changing application code.

**Why this priority**: Custom theme discovery makes the theme system extensible beyond built-in choices.

**Independent Test**: Add valid custom app and Markdown themes with display names to the expected theme locations, restart or refresh settings as appropriate, and confirm both appear in settings, validate correctly, and apply to their matching theme family only.

**Acceptance Scenarios**:

1. **Given** a valid custom app theme exists in the app themes folder, **When** the user opens app theme settings, **Then** the custom app theme appears with its display name.
2. **Given** a valid custom Markdown theme exists in the Markdown themes folder, **When** the user opens Markdown theme settings, **Then** the custom Markdown theme appears with its display name.
3. **Given** a custom theme is invalid, incomplete, unreadable, duplicated, or for the wrong theme family, **When** themes are loaded, **Then** the dashboard ignores or quarantines that theme, keeps usable themes available, and shows clear validation feedback.
4. **Given** the user selects a custom theme and saves settings, **When** the dashboard is reopened, **Then** the same custom theme selection is restored if the theme is still available.

---

### User Story 4 - Open Spec Kit Checklists By Hotkey (Priority: P4)

A dashboard user can press a dedicated hotkey to view Spec Kit checklists for the active feature, so quality gates and pending checklist items are accessible without leaving the dashboard.

**Why this priority**: Checklist access is requested separately from theming and supports the existing Spec Kit workflow.

**Independent Test**: Open the dashboard for a feature with checklists, press the checklist hotkey, and confirm available checklists are listed and readable with keyboard-only navigation.

**Acceptance Scenarios**:

1. **Given** the dashboard is open for a feature with one or more Spec Kit checklist files, **When** the user presses the checklist hotkey, **Then** the dashboard opens a checklist view showing the available checklists.
2. **Given** a checklist is selected, **When** the user opens it, **Then** checklist items, checked state, unchecked state, headings, and notes are readable.
3. **Given** no checklists are available for the active feature, **When** the user presses the checklist hotkey, **Then** the dashboard shows a clear non-fatal empty-state message.
4. **Given** the checklist view is closed, **When** the user returns to the dashboard, **Then** their prior dashboard context and selection are preserved.

### Edge Cases

- The app or operating environment does not expose a reliable light/dark preference; the `default` app theme must still resolve to a readable mode.
- A terminal cannot accurately display a configured color, background, or border style.
- A custom theme omits required settings, includes unknown future settings, uses duplicate identifiers, or has the same display name as another theme.
- A custom theme disappears after being selected and saved.
- A theme choice would make foreground and background colors indistinguishable.
- Alternate row shading is enabled by a custom theme but the table also has selection, warning, or error row styling.
- Markdown spacing settings would produce too much vertical whitespace for short terminal heights.
- Markdown element colors conflict with app-level background, selection, warning, or error colors.
- Theme folders are missing, empty, unreadable, or contain files for only one theme family.
- Theme settings are edited while the dashboard is running and visible views are already open.
- Checklist files are missing, empty, unreadable, malformed, or very large.
- The checklist hotkey conflicts with an existing command or user-configured binding.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The dashboard MUST support app themes as named bundles of basic TUI presentation settings with a stable identifier and user-facing display name.
- **FR-002**: App themes MUST cover comprehensive dashboard presentation settings, including table settings, foreground colors, background colors, border style, selected state, muted text, status colors, warnings, errors, and related readable UI roles.
- **FR-003**: App themes MUST include `default`, `light`, and `dark` built-in choices.
- **FR-004**: The `default` app theme MUST resolve to light or dark presentation according to the app-level setting when present, otherwise the operating environment preference when available, otherwise a readable fallback mode.
- **FR-005**: Users MUST be able to manually select `light` or `dark` app themes regardless of the app or operating environment preference.
- **FR-006**: App themes MUST support custom themes discovered from the app themes folder.
- **FR-007**: Alternate row shading MUST be off by default for built-in app themes unless the user explicitly enables it or selects a custom theme that declares it.
- **FR-008**: The dashboard MUST support Markdown themes as a separate theme family from app themes, with each Markdown theme having a stable identifier and user-facing display name.
- **FR-009**: Markdown themes MUST include `plain` and `default` built-in choices.
- **FR-010**: The `plain` Markdown theme MUST preserve the current Markdown rendering appearance as the compatibility baseline.
- **FR-011**: The `default` Markdown theme MUST make Markdown content easier for humans to consume than `plain`, including distinct styling for headings, emphasized text, links, inline code, code blocks, block quotes, lists, normal text, and muted text.
- **FR-012**: Markdown themes MUST support spacing controls between sections, headings, paragraphs, lists, and code blocks to improve human scanning, comprehension, and navigation.
- **FR-013**: Markdown theme rendering MUST honor the resolved app light/dark mode for contrast and readability unless the selected Markdown theme provides explicit compatible values for both modes.
- **FR-014**: Markdown themes MUST support custom themes discovered from the Markdown themes folder.
- **FR-015**: App themes and Markdown themes MUST be selected independently from in-app settings.
- **FR-016**: In-app settings MUST show built-in and custom app themes with display names, current selection, validation feedback, and save/discard behavior.
- **FR-017**: In-app settings MUST show built-in and custom Markdown themes with display names, current selection, validation feedback, and save/discard behavior.
- **FR-018**: Theme selections saved from in-app settings MUST persist across dashboard restarts.
- **FR-019**: If a saved custom theme is no longer available, the dashboard MUST fall back to the matching built-in default for that theme family and show clear feedback.
- **FR-020**: Invalid, unreadable, or incomplete custom themes MUST NOT prevent the dashboard from starting or from listing valid themes.
- **FR-021**: Unknown future custom theme settings MUST NOT prevent known valid settings in the same theme from being used.
- **FR-022**: Theme validation MUST reject or safely replace settings that would make required foreground and background content unreadable.
- **FR-023**: Changing an app theme or Markdown theme MUST update the affected visible dashboard surfaces without requiring a restart when the change can be safely applied live.
- **FR-024**: Markdown-backed constitution, full/detail document, and checklist views MUST use the selected Markdown theme.
- **FR-025**: Compact dashboard table cells MUST remain optimized for scanning and MUST NOT expand into full Markdown document spacing.
- **FR-026**: The dashboard MUST provide a dedicated hotkey command for viewing Spec Kit checklists for the active feature.
- **FR-027**: The checklist hotkey MUST participate in existing hotkey discovery, customization, and conflict handling.
- **FR-028**: The checklist view MUST list available checklists for the active feature and allow keyboard-only navigation to read them.
- **FR-029**: Checklist rendering MUST clearly distinguish headings, checked items, unchecked items, notes, and empty states.
- **FR-030**: Opening and closing theme settings or checklist views MUST preserve the user's prior dashboard selection and navigation context.
- **FR-031**: Theme and checklist failures MUST produce clear non-fatal feedback and preserve the last usable dashboard presentation.

### Key Entities

- **App Theme**: A named bundle of basic TUI presentation settings, including display name, identifier, light/dark behavior, table presentation, color roles, background roles, borders, row shading, and validation status.
- **Markdown Theme**: A named bundle of Markdown document rendering settings, including display name, identifier, element color roles, spacing rules, app-mode compatibility, and validation status.
- **Theme Family**: A classification that keeps app themes and Markdown themes separate so settings from one family are not applied to the other.
- **Theme Folder**: A user-accessible location containing custom theme definitions for one theme family.
- **Theme Selection**: The user's saved choice for app theme and Markdown theme, including fallback status when a selected custom theme is unavailable.
- **Resolved Display Mode**: The effective light or dark mode used by the dashboard after evaluating manual selection, app preference, operating environment preference, and fallback rules.
- **Checklist View**: A dashboard view that lists and displays Spec Kit checklist files for the active feature while preserving the user's previous dashboard context.
- **Theme Validation Feedback**: User-facing messages that explain invalid, unreadable, incomplete, duplicate, or unavailable theme definitions and the fallback applied.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can switch between built-in `default`, `light`, and `dark` app themes from in-app settings in under 30 seconds during a usability review.
- **SC-002**: 100% of dashboard table, status, detail, settings, and checklist surfaces use the selected app theme's applicable colors, backgrounds, and borders after the setting is applied.
- **SC-003**: Alternate row shading is off in 100% of built-in app theme baseline checks unless explicitly enabled by the user.
- **SC-004**: Users can switch between built-in `plain` and `default` Markdown themes from in-app settings in under 30 seconds during a usability review.
- **SC-005**: At least 90% of reviewed Markdown documents are rated easier to scan and understand with the `default` Markdown theme than with `plain`.
- **SC-006**: Valid custom app and Markdown themes added to theme folders appear in in-app settings with display names in 100% of discovery tests.
- **SC-007**: Invalid or missing custom themes never prevent the dashboard from starting during validation smoke checks.
- **SC-008**: Theme changes that are safe to apply live become visible in affected dashboard views within 2 seconds under normal local conditions.
- **SC-009**: Users can open Spec Kit checklists from the dashboard with a single hotkey action in under 2 seconds for features containing up to 10 checklist files.
- **SC-010**: Existing dashboard navigation, settings, Markdown rendering, constitution access, refresh, and quit workflows continue to pass smoke checks after theme and checklist hotkey behavior is added.

## Assumptions

- "Basic TUI app settings" refers to dashboard-wide presentation outside Markdown document rendering, including tables, borders, colors, backgrounds, selection, status, and related display roles.
- App themes and Markdown themes are intentionally separate because document readability settings have different roles and spacing needs than compact dashboard UI settings.
- The built-in app `default` theme is a resolver choice, not a third fixed palette; it follows app or operating environment light/dark preference when available.
- If neither app nor operating environment light/dark preference is available, the `default` app theme resolves to the existing project-preferred readable fallback.
- The built-in Markdown `plain` theme is the compatibility theme for current Markdown output, while Markdown `default` is a new human-readability-oriented theme.
- Custom themes live in theme folders discoverable by the dashboard; exact paths and file formats will be determined during planning.
- Theme selection belongs in the existing in-app settings experience rather than a separate first-run flow.
- The active feature for checklist viewing is the same feature context already used by the dashboard for Spec Kit artifacts.
