# Research: Markdown Rendering and Constitution Hotkey

## Decision: Use `NTokenizers.Extensions.Spectre.Console` for Markdown-backed document surfaces

**Rationale**: The feature request explicitly names `https://crwsolutions.github.io/NTokenizers.Extensions.Spectre.Console/`. The NuGet package describes stream-capable Spectre.Console rendering extensions for Markdown and other tokenized formats, matching the dashboard's existing Spectre.Console rendering stack. Keeping rendering inside Spectre avoids introducing a second terminal rendering model.

**Alternatives considered**:
- Plain Spectre.Console markup conversion: rejected because it would require hand-rolled Markdown parsing and would likely miss fenced code, lists, links, and malformed-input fallback behavior.
- Markdig plus custom Spectre rendering: viable but more custom code than needed for this user request.
- Render raw Markdown only: rejected because it does not satisfy the requested formatted Markdown behavior.

## Decision: Upgrade Spectre.Console to a compatible pinned version

**Rationale**: The dashboard currently references Spectre.Console 0.49.1. `NTokenizers.Extensions.Spectre.Console` 2.2.0 depends on Spectre.Console 0.54.0 or newer, so implementation must treat the Spectre upgrade as an intentional dependency change and verify dashboard rendering smoke tests afterward.

**Alternatives considered**:
- Use an older preview version of `NTokenizers.Extensions.Spectre.Console`: rejected because the stable 2.2.0 release is available and the plan should prefer a current stable package.
- Avoid the requested package to keep Spectre.Console pinned: rejected because the user specifically requested the package and the dependency upgrade is bounded to the Dashboard project.

## Decision: Keep Markdown rendering out of compact table cells

**Rationale**: The spec requires constitution and full/detail document views to render Markdown while compact table cells remain plain. Tables in this dashboard prioritize scan density and stable row height; applying rich Markdown there would expand rows, disrupt scrolling, and break compact navigation.

**Alternatives considered**:
- Render Markdown everywhere text appears: rejected because table cells would become unstable and less compact.
- Add a user setting to toggle table Markdown: rejected for this feature because it expands scope beyond the requested constitution/detail behavior.

## Decision: Read constitution content on each open

**Rationale**: FR-009 requires the current file contents after changes. On-demand reading avoids stale cached content and keeps behavior simple. The expected path is `.specify/memory/constitution.md` resolved under the active repository root.

**Alternatives considered**:
- Watch the constitution file continuously: rejected because the feature only requires freshness when opening the view, and the dashboard already has separate live config behavior.
- Cache until dashboard refresh: rejected because users could see stale constitution content after file changes.

## Decision: Fallback to escaped plain text with diagnostics when rendering fails

**Rationale**: The constitution requires safe failure and structured diagnostics. A Markdown renderer exception, malformed syntax, empty file, missing file, or unreadable file should never terminate the dashboard. Fallback plain text keeps content visible where possible and produces actionable feedback.

**Alternatives considered**:
- Fail the hotkey command on render errors: rejected because it violates safe failure and leaves users without readable content.
- Swallow render errors silently: rejected because diagnostics are mandatory for operationally significant IO/rendering failures.
