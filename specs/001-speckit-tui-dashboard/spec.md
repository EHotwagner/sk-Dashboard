# Feature Specification: Speckit TUI Dashboard

**Feature Branch**: `001-speckit-tui-dashboard`  
**Created**: 2026-04-25  
**Status**: Draft  
**Input**: User description: "i want a live dashboard for speckit. it should be a tui application that can run in am emacs vterm. it should always work even if the specs are not yet created. it should automatically detect the latest feature branch, checkout and show the status of the feature. it is also possible to manually checkout older feature branches if available. created user stories should be listed on the left side of the dashboard, selectable per keyboard. all is controllable by keyboards, hotkeys are configurable. similar to dired. you can move up and down features. plan has to be a separate pane i think, since it doesnt map to user stories. tasks flow from the user stories to the right as dag direct acyclic graph with some tasks depending on others. the status of the task has to be visible and selecting the node per keyboard shows a detailed view of all task information in a separate pane."

## Clarifications

### Session 2026-04-25

- Q: What branch switching policy should the dashboard use on startup? -> A: Always auto-checkout the latest feature branch on startup, and show an error only if checkout fails.
- Q: What task graph scope should be shown for a selected user story? -> A: Show selected story tasks plus their dependency chain, including cross-story dependencies.
- Q: How should task status values be displayed? -> A: Use only raw status text found in task artifacts, with no normalization.
- Q: What scope should hotkey configuration use? -> A: Hotkeys are persistently configurable globally for the user.
- Q: How should live dashboard updates behave? -> A: Automatically refresh when files or branch state change, plus support manual refresh.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Open a Useful Dashboard from Any Project State (Priority: P1)

A developer opens the dashboard inside a terminal session and immediately sees the current Speckit project state, even when no specifications, plans, or tasks have been created yet.

**Why this priority**: The dashboard must be dependable as the first command a developer runs in a new or partially initialized feature workflow.

**Independent Test**: Can be fully tested by opening the dashboard in a project with no feature specs and confirming it shows an empty-but-actionable state instead of failing.

**Acceptance Scenarios**:

1. **Given** a project with no specs directory or no feature specifications, **When** the developer opens the dashboard, **Then** the dashboard displays an empty feature state with available next actions and remains fully navigable.
2. **Given** a project with partial Speckit artifacts, **When** the developer opens the dashboard, **Then** available artifacts are shown and missing artifacts are clearly indicated without blocking the dashboard.
3. **Given** Speckit artifacts or branch state changes while the dashboard is open, **When** the change is detected, **Then** the dashboard refreshes visible status automatically while still allowing manual refresh.

---

### User Story 2 - Review the Latest Feature Status Automatically (Priority: P1)

A developer opens the dashboard and the latest available feature branch is selected automatically so they can review its status without first remembering branch names or file locations.

**Why this priority**: Automatic feature detection makes the dashboard useful as a live status surface instead of another manual navigation step.

**Independent Test**: Can be fully tested by creating multiple feature branches with different identifiers, opening the dashboard, and confirming the latest feature is selected and summarized.

**Acceptance Scenarios**:

1. **Given** multiple feature branches exist, **When** the dashboard starts, **Then** it selects the latest feature branch and shows the selected feature's specification, plan, and task status where available.
2. **Given** the latest feature branch cannot be checked out because of local project constraints, **When** the dashboard starts, **Then** it shows the checkout failure clearly and preserves enough visible context for the developer to resolve it.

---

### User Story 3 - Navigate Features and User Stories by Keyboard (Priority: P2)

A developer uses keyboard controls to move through available feature branches and user stories, with user stories listed in a dedicated left-side pane.

**Why this priority**: Keyboard-first navigation is central to the requested workflow and makes the dashboard practical inside terminal environments.

**Independent Test**: Can be fully tested by opening a project with several features and user stories, then selecting each item using only keyboard input.

**Acceptance Scenarios**:

1. **Given** feature branches are available, **When** the developer moves up and down the feature list, **Then** the selected feature changes and the visible status updates to match it.
2. **Given** a selected feature has user stories, **When** the developer navigates the left-side story list, **Then** the selected story is highlighted and its related tasks become the focus of the task view.
3. **Given** older feature branches are available, **When** the developer chooses one manually, **Then** the dashboard switches context to that feature and shows its status.

---

### User Story 4 - Inspect Plan and Task Graph Details (Priority: P2)

A developer reviews the selected feature's plan in a separate pane and follows the related tasks as a visible directed acyclic graph flowing from user stories toward dependent work.

**Why this priority**: Speckit plans do not map one-to-one to user stories, while tasks often depend on one another; separate plan and graph views keep both relationships understandable.

**Independent Test**: Can be fully tested by using a feature with a plan and tasks that include dependencies, then confirming the plan remains separately visible and task dependencies are shown as a non-cyclic graph.

**Acceptance Scenarios**:

1. **Given** a selected feature includes a plan, **When** the dashboard renders the feature, **Then** the plan appears in its own pane separate from the user story list.
2. **Given** a selected user story has tasks with dependencies, **When** the task pane is shown, **Then** tasks for the selected story and their dependency chain flow from the story toward dependent tasks, including dependencies connected to other stories.
3. **Given** a task node is selected, **When** the developer requests details, **Then** a detail pane shows all available task information including title, description, raw status text, dependencies, story relationship, and source location.

---

### User Story 5 - Customize Keyboard Controls (Priority: P3)

A developer configures dashboard hotkeys so the application can match their preferred terminal workflow.

**Why this priority**: Configurable controls improve usability for keyboard-heavy workflows, but the dashboard remains valuable with sensible defaults.

**Independent Test**: Can be fully tested by changing a hotkey configuration, restarting or reloading the dashboard, and confirming the new binding performs the intended command.

**Acceptance Scenarios**:

1. **Given** default hotkeys are available, **When** the developer uses the dashboard without custom configuration, **Then** all primary commands are reachable by keyboard.
2. **Given** the developer has configured a global custom hotkey, **When** that key is pressed in any project, **Then** the configured action runs and any conflicting binding is reported clearly.

### Edge Cases

- No Speckit artifacts exist yet in the project.
- A feature branch exists but its specification, plan, or task artifacts are missing.
- Multiple branches appear to be feature branches with the same numeric or timestamp order.
- The project has local changes that prevent safely switching feature branches.
- Task dependency data is missing, malformed, references unknown tasks, or contains a cycle.
- Terminal dimensions are too small to show all panes at once.
- A configured hotkey conflicts with another command or uses an unsupported key sequence.
- A selected task or user story references content that no longer exists.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The dashboard MUST start successfully in a terminal session even when no Speckit specifications, plans, tasks, or feature branches exist.
- **FR-002**: The dashboard MUST present a clear empty state when no Speckit feature data exists, including at minimum visible actions for refresh, quit, feature branch selection when branches exist, and guidance to create or initialize Speckit artifacts when none exist.
- **FR-003**: The dashboard MUST detect available feature branches and identify the latest feature by the project's feature ordering convention.
- **FR-004**: The dashboard MUST automatically attempt to check out the latest available feature branch when it starts and MUST show a clear error if checkout fails.
- **FR-005**: Users MUST be able to manually select older feature branches when they are available.
- **FR-006**: The dashboard MUST show the currently selected feature's status, including whether specification, plan, task, and checklist artifacts are missing, present, unreadable, malformed, or complete according to detectable artifact-specific checks.
- **FR-007**: The dashboard MUST list user stories for the selected feature in a dedicated left-side pane.
- **FR-008**: Users MUST be able to move through features, user stories, task nodes, and detail panes using keyboard controls only.
- **FR-009**: The dashboard MUST provide globally persistent user-level hotkey configuration for primary commands, including feature navigation, story navigation, task navigation, pane focus changes, refresh, branch selection, and quit.
- **FR-010**: The dashboard MUST provide sensible default hotkeys for all primary commands before any custom configuration exists.
- **FR-011**: The dashboard MUST show the selected feature's plan in a pane separate from the user story list and task graph.
- **FR-012**: The dashboard MUST show tasks related to the selected story and their dependency chain as a directed acyclic graph when task relationship data is available, including cross-story dependencies required to understand the selected story's work.
- **FR-013**: The dashboard MUST visibly show each task's status in the graph using the raw status text found in task artifacts, without normalizing status values.
- **FR-014**: Users MUST be able to select task nodes by keyboard.
- **FR-015**: When a task node is selected, the dashboard MUST show a detail pane containing all available task information, including title, description, raw status text, dependencies, related user story, and source location.
- **FR-016**: The dashboard MUST detect and clearly report malformed task relationships, including missing references and cyclic dependencies, while continuing to show recoverable task information.
- **FR-017**: The dashboard MUST automatically refresh displayed status when underlying Speckit artifacts or branch state changes and MUST also provide a manual refresh command.
- **FR-018**: The dashboard MUST remain usable when the terminal is resized, including by preserving access to all panes through focus changes, scrolling, or alternate compact layouts.
- **FR-019**: The dashboard MUST clearly indicate any action that cannot be completed because of local project state, such as branch switching blocked by uncommitted changes.
- **FR-020**: The dashboard MUST support operation in terminal environments that provide keyboard input and text rendering compatible with common editor-integrated terminals.

### Key Entities

- **Feature**: A Speckit feature branch or feature directory, including its identifier, display name, order, selected state, and artifact availability.
- **Feature Status**: The summarized readiness of a feature based on available specification, plan, task, checklist, and validation information.
- **User Story**: A prioritized scenario from the feature specification, including title, priority, acceptance scenarios, and related tasks.
- **Plan**: Feature planning content that applies to the feature as a whole rather than to a single user story.
- **Task**: A unit of work from the task list, including title, description, raw status text, dependencies, related story, and source location.
- **Task Graph**: The visible relationship model showing tasks for the selected story plus their dependency chain, including dependencies connected to other stories.
- **Hotkey Binding**: A globally persistent user-level keyboard shortcut mapped to a dashboard command.
- **Pane**: A dashboard region with focusable content, such as features, stories, plan, task graph, and task details.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can open the dashboard from a project with no Speckit artifacts and reach a stable empty state in under 3 seconds.
- **SC-002**: In a project with at least 10 feature branches, the dashboard selects and displays the latest feature status in under 5 seconds.
- **SC-003**: A developer can switch from the latest feature to an older feature and view that feature's status using only keyboard input in under 15 seconds.
- **SC-004**: For a feature with at least 5 user stories and 30 tasks, a developer can select any listed story and inspect a related task's full details using only keyboard input in under 30 seconds.
- **SC-005**: 95% of malformed or missing Speckit artifact scenarios produce a visible, actionable status instead of exiting unexpectedly.
- **SC-006**: 90% of first-time users familiar with keyboard file navigation can identify the selected feature, selected story, plan pane, task graph, and task detail pane within 2 minutes.
- **SC-007**: Custom hotkey changes are reflected accurately for all primary commands with zero unresolved binding conflicts after configuration validation.
- **SC-008**: Visible dashboard status reflects detected Speckit artifact or branch changes within 2 seconds in 95% of normal local project updates.

## Assumptions

- The primary users are developers working inside local Speckit-enabled repositories.
- "Latest feature branch" follows the repository's feature ordering convention, such as sequential numeric prefixes or timestamp prefixes.
- The dashboard prioritizes automatic checkout of the latest feature branch on startup; checkout failures are handled by visible errors rather than silent fallback.
- Missing Speckit artifacts are expected during early workflow stages and are treated as normal states.
- Artifact completeness is limited to locally detectable checks: required files exist, can be read, parse without known structural errors, and expose expected sections or task/checklist markers when those formats are known.
- The dashboard defaults to a keyboard navigation model inspired by directory browsers, with configurable bindings for users who prefer different keys.
- Hotkey customizations apply globally for the user across projects.
- Live updates are expected for local file and branch state changes, with manual refresh available as a fallback.
- Task graph rendering depends on available task relationship data; when relationships are incomplete, the dashboard still shows available tasks and reports the issue.
- Selecting a user story narrows the task graph to that story's work plus dependencies needed to understand or complete that work.
