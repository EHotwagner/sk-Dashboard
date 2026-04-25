# Unsupported Scope Diagnostics

The dashboard treats these states as recoverable diagnostics:
- Missing `specs/` directory or no feature artifact directories.
- Missing `spec.md`, `plan.md`, `tasks.md`, or checklist files for a feature.
- Unreadable or malformed artifact content.
- Git checkout failures caused by local repository state.
- Terminal layouts too small to show every pane at once.

The first implementation surface reports these states in the dashboard
snapshot diagnostics instead of exiting.
