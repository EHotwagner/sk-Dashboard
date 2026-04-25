# Research: Configurable Dashboard UI

## Decision: Keep UI preferences in the existing dashboard preferences file

**Rationale**: Users already configure dashboard hotkeys through a global preferences path. Adding UI settings to the same file keeps terminal behavior in one place and allows one reload action to refresh both key bindings and presentation settings.

**Alternatives considered**:

- Separate UI preference file: rejected because users would need to discover and manage two files for one dashboard experience.
- Support both single-file and separate-file modes: rejected for this feature because it increases validation and documentation complexity without clear user value.

## Decision: Represent layout mode as `widescreen`, `vertical`, or `auto`

**Rationale**: The user asked for widescreen versus vertical layout options, and clarification selected a manual-plus-automatic default model. `auto` gives good first-run behavior while still allowing explicit control.

**Alternatives considered**:

- Manual-only mode: rejected because default behavior would remain brittle for users switching between full terminal and editor panes.
- Automatic-only mode: rejected because users asked for layout options and may prefer one stable arrangement.

## Decision: Use 120 terminal columns as the automatic layout threshold

**Rationale**: 120 columns gives enough space for navigation context and detail context to appear side by side without severe truncation. Below that width, a vertical arrangement is more readable and easier to test.

**Alternatives considered**:

- 100 columns: rejected because it is likely too narrow for simultaneous feature/story and plan/task/detail panes.
- 140 columns: rejected because it would delay widescreen mode on many common terminals that can reasonably support side-by-side sections.

## Decision: Accept named terminal colors and hex RGB color values

**Rationale**: Named terminal colors are readable and easy to configure; hex RGB colors allow exact theme matching for users with custom palettes.

**Alternatives considered**:

- Named colors only: rejected because users could not match custom themes precisely.
- Hex colors only: rejected because simple configs would become less readable.
- Style tokens in the same setting: deferred to avoid mixing color and typography semantics in the first iteration.

## Decision: Warn and fall back for low-contrast foreground/background pairs

**Rationale**: Readability is a core reason for the feature. Applying low-contrast pairs would satisfy configuration but fail the dashboard's purpose. Diagnostics plus fallback keep the user informed and the app usable.

**Alternatives considered**:

- Apply low-contrast values anyway: rejected because it can make the dashboard unreadable.
- Warn only: rejected because warning text may itself be hard to see after low-contrast settings are applied.

## Decision: Validate preferences as recoverable diagnostics

**Rationale**: Preference files are user-edited and easy to mistype. Invalid values must not prevent users from checking project status. Valid values should still apply when nearby values are invalid.

**Alternatives considered**:

- Fail startup on invalid preferences: rejected by the spec's safe-default requirement.
- Ignore invalid preferences silently: rejected because users need actionable feedback.
