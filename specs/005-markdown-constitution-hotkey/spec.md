# Feature Specification: Markdown Rendering and Constitution Hotkey

**Feature Branch**: `005-markdown-constitution-hotkey`  
**Created**: 2026-04-25  
**Status**: Draft  
**Input**: User description: "use https://crwsolutions.github.io/NTokenizers.Extensions.Spectre.Console/ to render markdown content. also add a hotkey to display the current constitution.md"

## Change Classification

**Tier**: Tier 1 (contracted change)

**Reason**: This changes observable dashboard behavior by rendering Markdown-formatted content in a richer display and adds a user-facing hotkey command for viewing the current project constitution. It may also affect public hotkey command identifiers, rendering behavior, and documentation.

**Public API Impact**: Expected if the hotkey command registry, command identifiers, dashboard state, or render contracts are exposed through public `.fsi` surfaces.

**Verification Approach**: Validate through semantic tests for public hotkey and state contracts, rendered-output smoke tests for Markdown content, and interactive behavior tests for opening, scrolling, and closing the constitution view.

## Clarifications

### Session 2026-04-25

- Q: Which dashboard surfaces should render Markdown formatting? -> A: Render Markdown in the constitution view and full/detail document views, but keep table cells compact/plain.
- Q: What default hotkey should open the constitution view? -> A: Use `C` as the default constitution hotkey.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Read Formatted Markdown Content (Priority: P1)

As a dashboard user, I want Markdown content shown with readable terminal formatting in constitution and full/detail document views so that project documents are easier to scan than raw Markdown text.

**Why this priority**: Markdown rendering is the core requested behavior and improves every dashboard surface that displays Markdown documents.

**Independent Test**: Can be fully tested by opening the constitution view and an existing full/detail document view, then confirming headings, lists, emphasis, links, and code blocks are displayed as readable formatted content without exposing distracting markup while table cells remain compact.

**Acceptance Scenarios**:

1. **Given** a constitution or full/detail document view displays Markdown content with headings and lists, **When** the surface is opened, **Then** the user sees formatted headings and list structure instead of an unprocessed wall of Markdown syntax.
2. **Given** Markdown content contains inline emphasis, code spans, links, and fenced code blocks, **When** the content is rendered, **Then** each element remains readable and distinguishable in the terminal.
3. **Given** Markdown content includes unsupported or malformed syntax, **When** the content is rendered, **Then** the dashboard still shows the underlying text in a readable form and does not crash.
4. **Given** a compact dashboard table contains Markdown-like text, **When** the table is rendered, **Then** table cells remain compact/plain and do not expand into full Markdown formatting.

---

### User Story 2 - Open Current Constitution by Hotkey (Priority: P2)

As a dashboard user, I want a keyboard shortcut that opens the current `constitution.md` so that I can quickly check project rules without leaving the dashboard.

**Why this priority**: The constitution governs project workflow and quality gates; fast access reduces context switching during planning and implementation.

**Independent Test**: Can be fully tested by launching the dashboard in a repository with a constitution file, pressing the assigned hotkey, and confirming the current constitution opens in a readable view.

**Acceptance Scenarios**:

1. **Given** the dashboard is open in a project with a current constitution file, **When** the user presses the constitution hotkey, **Then** the dashboard displays the constitution content in a dedicated readable view.
2. **Given** the constitution view is open, **When** the user closes it, **Then** the dashboard returns to the previous context without losing the selected feature, story, task, or navigation position.
3. **Given** the constitution content is longer than the visible terminal area, **When** the user navigates within the view, **Then** the user can reach the full document content using keyboard-only controls.

---

### User Story 3 - Handle Missing or Changed Constitution Safely (Priority: P3)

As a dashboard user, I want clear feedback if the constitution cannot be shown so that I understand whether the file is missing, unreadable, or changed.

**Why this priority**: Safe failure keeps the hotkey useful across incomplete repositories and prevents document access from destabilizing the dashboard.

**Independent Test**: Can be fully tested by opening the dashboard in repositories with present, missing, unreadable, and modified constitution files and observing the resulting messages and recovery behavior.

**Acceptance Scenarios**:

1. **Given** no constitution file exists at the expected project location, **When** the user presses the constitution hotkey, **Then** the dashboard shows a clear non-fatal message that the constitution is unavailable.
2. **Given** the constitution file cannot be read, **When** the user presses the constitution hotkey, **Then** the dashboard shows a clear non-fatal error with the attempted file location.
3. **Given** the constitution changes while the dashboard is running, **When** the user opens the constitution view again, **Then** the displayed content reflects the current file contents.

### Edge Cases

- The constitution file is empty: show an explicit empty-document message rather than a blank or broken view.
- The constitution file contains very long lines, large code blocks, nested lists, or tables: preserve readability and allow keyboard navigation without layout corruption.
- The terminal is too narrow or too short for comfortable display: keep controls usable and avoid overlapping content.
- The selected constitution hotkey conflicts with an existing dashboard command: use the existing hotkey conflict handling so users receive clear feedback and no command becomes ambiguous.
- Markdown rendering fails for a document: show readable fallback text and emit diagnostics with enough context to troubleshoot.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The dashboard MUST render Markdown-backed constitution and full/detail document content as formatted terminal content for headings, paragraphs, lists, emphasis, inline code, links, and fenced code blocks.
- **FR-002**: The dashboard MUST preserve readable fallback output when Markdown content contains unsupported syntax, malformed syntax, or content that cannot be fully formatted.
- **FR-003**: Users MUST be able to open the current project constitution using a dedicated hotkey command from the dashboard.
- **FR-003a**: The dedicated constitution command MUST use `C` as its default key binding unless the user overrides it through dashboard hotkey preferences.
- **FR-004**: The constitution view MUST display the current constitution file contents using the same Markdown rendering behavior as other Markdown-backed surfaces.
- **FR-005**: Users MUST be able to close the constitution view and return to their prior dashboard context without losing active selection or navigation position.
- **FR-006**: Users MUST be able to navigate the full constitution content by keyboard when it exceeds the visible terminal area.
- **FR-007**: The dashboard MUST show clear non-fatal feedback when the constitution file is missing, empty, unreadable, or cannot be rendered as formatted Markdown.
- **FR-008**: The constitution hotkey MUST participate in the dashboard's existing hotkey discovery, help, customization, and conflict behavior.
- **FR-009**: The dashboard MUST read the constitution from the current project context each time the user opens the constitution view so users do not see stale content after file changes.
- **FR-010**: Operational diagnostics MUST identify constitution access and Markdown rendering failures without exposing unrelated sensitive environment details.
- **FR-011**: Compact dashboard tables MUST keep Markdown-like cell content plain and compact rather than expanding it into full Markdown formatting.

FR-003a is intentionally grouped under FR-003 because it constrains the default binding for the same constitution-open command.

### Key Entities

- **Markdown Document**: A text document containing Markdown content to be displayed in the dashboard; includes raw content, source location when available, rendered display state, and fallback status.
- **Constitution View**: A dashboard view that presents the current project constitution, tracks scroll/navigation position, and returns to the previous dashboard context when closed.
- **Hotkey Command**: A keyboard-triggered dashboard action with an identifier, display label, default key binding, customization behavior, and conflict status.
- **Render Failure Diagnostic**: A non-fatal diagnostic event describing why Markdown formatting or constitution loading could not be completed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 95% of common Markdown elements in representative constitution and full/detail document views render in a visibly structured form during smoke testing.
- **SC-002**: Users can open the current constitution from the dashboard with a single hotkey action in under 2 seconds for constitution files up to 2,000 lines.
- **SC-003**: Users can close the constitution view and return to their prior dashboard context with no selection loss in 100% of tested navigation scenarios.
- **SC-004**: Missing, empty, unreadable, or malformed constitution cases produce a clear non-fatal message in 100% of tested cases.
- **SC-005**: Markdown rendering and constitution access failures never terminate the dashboard during the defined smoke and semantic test scenarios.

## Assumptions

- The "current constitution" is the constitution file resolved from the active project context, with `.specify/memory/constitution.md` treated as the expected source in this repository.
- The implementation planning phase will account for the requested Markdown rendering resource at `https://crwsolutions.github.io/NTokenizers.Extensions.Spectre.Console/`.
- The constitution view should follow the existing dashboard detail-view and hotkey patterns rather than introducing a separate mode of operation.
- The default constitution hotkey follows the existing uppercase full/detail document hotkey pattern.
- Compact table cells remain optimized for scanning and navigation, so Markdown formatting is reserved for constitution and full/detail document views.
- Existing dashboard settings and hotkey conflict behavior remain authoritative for displaying and customizing the new command.
- This feature does not change the constitution content, governance rules, or Spec Kit command behavior; it only improves display and access.
