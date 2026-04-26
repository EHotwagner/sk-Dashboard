# Evidence Plan

Tier 1 evidence for this feature is recorded with real filesystem fixtures, Expecto semantic tests, dashboard smoke runs through `--keys`, and public surface baselines.

- Core contract evidence: `dotnet test sk-Dashboard.sln` plus `readiness/fsi-session.txt`.
- Dashboard vertical-slice evidence: scripted dashboard runs under `readiness/dashboard-smoke/`.
- Build/test evidence: `readiness/dotnet-build.txt` and `readiness/dotnet-test.txt`.
- Audit evidence: `readiness/graph-only-final.txt` and `readiness/audit-final.txt`.

No synthetic evidence is accepted for completed tasks in this feature.
