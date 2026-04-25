# Quickstart Smoke Results

- Dashboard launch and constitution hotkey: `dashboard-smoke/constitution-open.txt`
- Constitution scroll and close command path: `dashboard-smoke/constitution-scroll-close.txt`
- Missing constitution fallback: `failures/missing-constitution.txt`
- Empty constitution fallback: `failures/empty-constitution.txt`
- 2,000-line constitution timing: `dashboard-smoke/constitution-2000-lines.time` (`elapsed=0.608` in this run)
- Semantic and smoke tests: `dotnet-test.txt`

The tests use real temporary filesystem repositories for missing, empty, malformed/Markdown-like, and changed-on-reopen cases. Compact table Markdown-like content is exercised through the table renderable path.
