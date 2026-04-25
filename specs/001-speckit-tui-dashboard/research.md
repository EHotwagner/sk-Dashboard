# Research: Speckit TUI Dashboard

## Decision: Use F# on .NET 10

**Rationale**: The repository is already an F# solution and the user explicitly requested .NET 10. The local SDK is available as `10.0.104`, so planning can target `net10.0` without a toolchain gap.

**Alternatives considered**: Staying on the current `net9.0` would reduce project-file churn but conflicts with the requested target. Rewriting in C# would add no clear benefit and would discard the existing F# scaffold.

## Decision: Use Spectre.Console for the terminal UI

**Rationale**: Spectre.Console provides mature .NET terminal rendering primitives such as layouts, panels, tables, live display, colors, and markup while remaining compatible with ordinary terminal streams. It is a pragmatic fit for an Emacs vterm target when paired with conservative keyboard input handling and no dependency on mouse or GUI APIs.

**Alternatives considered**: Terminal.Gui provides a fuller widget model but is heavier and can be more sensitive to terminal capabilities. A raw ANSI renderer would maximize control but would slow delivery and increase risk around resizing, layout, and rendering polish.

## Decision: Keep domain/state logic in a core library

**Rationale**: Feature discovery, artifact parsing, graph validation, status summaries, and hotkey validation are the highest-risk behaviors and can be tested deterministically without launching an interactive terminal. A separate core library also keeps the console app focused on input, rendering, and refresh orchestration.

**Alternatives considered**: A single console project would be simpler initially but would couple parsing and process interactions to UI rendering, making malformed-artifact cases and graph behavior harder to test.

## Decision: Use Git CLI process calls for branch discovery and checkout

**Rationale**: Speckit workflows are Git-centered and the dashboard must respect local checkout behavior exactly, including failures caused by uncommitted changes. Calling the installed Git executable preserves user-visible Git semantics and avoids binding to a narrower library abstraction.

**Alternatives considered**: LibGit2Sharp would provide in-process Git access but adds native dependency concerns and may diverge from command-line Git behavior in edge cases important to this feature.

## Decision: Determine the latest feature branch by parsed Speckit ordering

**Rationale**: The spec assumes feature branches follow a project ordering convention, such as sequential numeric or timestamp prefixes. Branch names should be parsed into an order key when possible, then sorted by order key, with deterministic tie-breaking by branch name and last available Git metadata when needed.

**Alternatives considered**: Sorting lexicographically is simple but can misorder numeric prefixes. Sorting only by commit time can select recently edited old branches rather than the latest feature identifier.

## Decision: Treat missing and malformed artifacts as data, not fatal errors

**Rationale**: The dashboard must work before specs exist and while artifacts are partial. Artifact readers should return structured warnings/errors alongside partial models so the renderer can show actionable status without terminating.

**Alternatives considered**: Failing fast on missing files is appropriate for build tools but violates the dashboard's primary empty-state requirement.

## Decision: Build task graph with explicit diagnostics

**Rationale**: The task graph must include selected-story tasks plus dependency chains, including cross-story dependencies. Missing references and cycles must be reported clearly while recoverable task information remains visible. The graph builder should therefore return graph nodes/edges plus diagnostics rather than a single success/failure value.

**Alternatives considered**: Hiding malformed relationships would keep the view clean but would prevent users from fixing broken task metadata. Rejecting the entire graph on any issue would be too brittle for partial artifacts.

## Decision: Store hotkeys globally in user config

**Rationale**: Requirements call for globally persistent user-level hotkeys. Use an XDG-compatible path such as `$XDG_CONFIG_HOME/sk-dashboard/hotkeys.json` on Unix-like systems, falling back to the .NET application data folder when XDG is unavailable. Configuration validation reports conflicts and unsupported key sequences before activating changes.

**Alternatives considered**: Repository-local config would be useful for team defaults but does not satisfy global user preference. Environment variables are too limited for a full command map.

## Decision: Use file watchers plus polling fallback for live refresh

**Rationale**: FileSystemWatcher can detect normal local artifact updates, while periodic refresh can cover missed events, Git branch movement, and editor-save patterns. The UI state reducer should coalesce events to avoid excessive re-rendering.

**Alternatives considered**: Polling only is simpler but may miss the 2-second refresh goal unless it runs unnecessarily often. Watchers only can miss Git state changes and platform-specific file events.
