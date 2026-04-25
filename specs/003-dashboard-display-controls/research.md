# Research: Dashboard Display Controls

## Decision: Use package/assembly metadata for the dashboard version, with a stable fallback

**Rationale**: `Directory.Build.props` already owns the package version used when packing and installing the global tool. The dashboard should resolve its displayed version from the installed assembly/package metadata so the header reflects the build users are actually running. When metadata is unavailable, the header still reserves and shows a fallback such as `vunknown` to satisfy the visibility requirement without misleading users.

**Alternatives considered**: Hardcoding the version in rendering code was rejected because it would drift from packaged builds. Reading `Directory.Build.props` at runtime was rejected because installed tools may not run from the source checkout.

## Decision: Add explicit row stripe color roles to the existing UI preference model

**Rationale**: The current preference contract already supports named visual roles with validation diagnostics and safe defaults. Adding stripe roles keeps alternating row colors configurable without introducing a second theme mechanism. Stripe styles apply only to non-selected, non-active, non-warning, and non-error data rows so stateful styling remains dominant.

**Alternatives considered**: Fixed hardcoded stripe colors were rejected because the spec requires configurable stripe colors. Deriving stripes from the selected or muted role was rejected because contrast can become poor in custom themes and diagnostics would be less actionable.

## Decision: Treat row striping as renderer presentation, not domain state

**Rationale**: Alternating backgrounds depend on visible table row order and should not affect sorting, filtering, selection, artifact parsing, or task graph state. Applying stripes in table builders keeps the behavior close to the Spectre.Console rendering code and allows simple precedence tests for selected/active/diagnostic rows.

**Alternatives considered**: Persisting an alternating row state on each model entity was rejected because it duplicates view order in the domain model and risks stale state after refresh or selection changes.

## Decision: Model full-screen views as a transient modal target in dashboard state

**Rationale**: The feature requires modal detail views that show exactly one target type and return to the same selection. A transient `FullScreenTarget`/modal state lets hotkeys open feature, story, plan, or task views without changing selected IDs. Closing the modal clears only that transient state.

**Alternatives considered**: Reusing the existing details pane was rejected because it remains constrained by the multi-pane layout. Combining all selected objects into one expanded screen was rejected because the spec requires exactly one requested target type at a time.

## Decision: Use existing hotkey preference loading for full-screen commands

**Rationale**: Full-screen commands are keyboard-only commands and must be compatible with configurable hotkeys. Adding command IDs to the current `DashboardCommand` union and default bindings keeps parsing, validation, diagnostics, scripted `--keys` smoke checks, and documentation consistent.

**Alternatives considered**: Handling hardcoded keys directly in `Input.fs` was rejected because it bypasses user-configurable bindings and would create a second command path.

## Decision: Combine parsed fields with source artifact text already associated with the selected target

**Rationale**: Parsed fields provide scannable information while raw artifact text provides full context. The plan, tasks, and spec artifacts are already loaded from the selected feature root, so full-screen views can read associated source text and fail safely when files are missing or unreadable.

**Alternatives considered**: Searching unrelated repository files was rejected because the spec scopes full-screen views to source text associated with the selected target. Showing only raw text was rejected because the user asked for all available information, including parsed dashboard data.
