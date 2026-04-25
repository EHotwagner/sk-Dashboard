# Data Model: Speckit TUI Dashboard

## Feature

Represents a Speckit feature branch or feature directory.

Fields:
- `id`: stable identifier parsed from branch or directory name.
- `branchName`: Git branch name when available.
- `displayName`: human-readable feature name.
- `orderKey`: parsed numeric, timestamp, or fallback ordering key.
- `isSelected`: whether this feature is the active dashboard selection.
- `artifactRoot`: path to the feature's specs directory when present.
- `checkoutState`: current checkout status and error details if checkout failed.
- `status`: current `FeatureStatus`.

Validation:
- Feature IDs must be non-empty when parsed from a branch or directory.
- Multiple features with the same order key must be deterministically ordered.
- A feature may exist without artifacts and must remain selectable if branch data exists.

## FeatureStatus

Summarizes readiness and visible project state for a feature.

Fields:
- `specState`: present, missing, unreadable, or malformed.
- `planState`: present, missing, unreadable, or malformed.
- `tasksState`: present, missing, unreadable, malformed, or graph-invalid.
- `checklistState`: present, missing, unreadable, or malformed.
- `diagnostics`: recoverable warnings and errors.
- `lastRefreshedAt`: timestamp for the current status snapshot.

Validation:
- Missing artifacts are valid status values.
- Diagnostics must preserve source paths when known.

## UserStory

Represents a prioritized scenario from a feature specification.

Fields:
- `id`: stable story identifier derived from heading order or explicit label.
- `title`: story title.
- `priority`: raw priority text, such as `P1`.
- `description`: story narrative.
- `acceptanceScenarios`: scenario text grouped under the story.
- `sourceLocation`: file path and heading/line reference when available.

Validation:
- A story may have no related tasks yet.
- The original story priority text should be preserved.

## Plan

Represents feature-wide planning content.

Fields:
- `path`: source plan path.
- `summary`: extracted summary section when present.
- `technicalContext`: extracted technical context when present.
- `constitutionCheck`: extracted gate status when present.
- `rawContent`: full plan text for pane rendering.
- `diagnostics`: warnings for missing or malformed sections.

Validation:
- Plan content is not required to map to individual user stories.
- Missing plan produces a normal empty plan state.

## Task

Represents a task parsed from Speckit task artifacts.

Fields:
- `id`: task identifier.
- `title`: task title.
- `description`: task detail text when available.
- `rawStatus`: status text exactly as found in task artifacts.
- `dependencies`: task IDs referenced as dependencies.
- `relatedStoryId`: linked user story identifier when available.
- `sourceLocation`: file path and line/reference.
- `metadata`: optional raw task metadata for future Speckit formats.

Validation:
- `rawStatus` must not be normalized or remapped.
- Unknown dependencies produce graph diagnostics instead of deleting the task.
- Tasks without a story remain visible when required by dependency chains.

## TaskGraph

Visible relationship model for a selected story.

Fields:
- `selectedStoryId`: story currently driving graph scope.
- `nodes`: tasks for the story plus dependency-chain tasks.
- `edges`: dependency relationships.
- `diagnostics`: missing references, cycles, duplicate IDs, and unsupported metadata.
- `selectedTaskId`: focused graph node when present.

Validation:
- A valid graph is directed and acyclic.
- Cycles must be reported while still showing recoverable nodes.
- Cross-story dependencies are included when needed to understand selected story work.

## HotkeyBinding

Globally persistent keyboard shortcut mapped to a dashboard command.

Fields:
- `command`: command identifier.
- `keySequence`: normalized key sequence string.
- `scope`: global command scope, initially `dashboard`.
- `source`: default or user configuration.

Validation:
- Every primary command must have an active binding.
- User bindings override defaults only after conflict validation.
- Unsupported key sequences and duplicate active bindings are diagnostics.

## Pane

Focusable dashboard region.

Fields:
- `id`: pane identifier.
- `title`: display label.
- `kind`: features, stories, plan, task graph, details, or diagnostics.
- `focusState`: focused or unfocused.
- `scrollOffset`: current scroll position when applicable.
- `selection`: pane-specific selected item ID.

Validation:
- At least one pane must be focusable in all layouts.
- Compact layouts must preserve access to every pane through focus changes or scrolling.
