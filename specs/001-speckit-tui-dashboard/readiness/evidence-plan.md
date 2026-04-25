# Evidence Plan

Feature tier: T1 for the command-line dashboard surface and Core public
contracts used by the dashboard.

Affected public surfaces:
- `src/Core/*.fsi`
- `src/Dashboard/Program.fs` command-line entry point
- `scripts/prelude.fsx`

Evidence obligations:
- Build and tests: `dotnet build sk-Dashboard.sln`, `dotnet test sk-Dashboard.sln`
- Public API: FSI transcript saved as `readiness/fsi-session.txt`
- Story slices: user-reachable dashboard smoke transcripts saved under
  `readiness/` before marking `[US*]` implementation tasks `[X]`
- Graph/evidence audit: `.specify/extensions/evidence/scripts/bash/run-audit.sh`
  before final completion
