# Task Graph — 003-dashboard-display-controls

## ✓ Graph is acyclic and consistent

## Status counts (effective)

| Status | Count |
|--------|-------|
| [X] done | 44 |
| [S] synthetic | 0 |
| [S*] auto-synthetic | 0 |

## Graph

```mermaid
graph TD
  T001["T001 Confirm active feature metadata points to `specs/0"]:::done
  T002["T002 Create `specs/003-dashboard-display-controls/readi"]:::done
  T003["T003 Record Tier 1 public-surface impact for version di"]:::done
  T004["T004 Add a display-controls evidence plan covering FSI,"]:::done
  T005["T005 Draft `.fsi` public surface updates for new versio"]:::done
  T006["T006 Add failing semantic tests for command IDs, defaul"]:::done
  T007["T007 Add failing dashboard state reducer tests for open"]:::done
  T008["T008 Add failing rendering smoke tests for header versi"]:::done
  T009["T009 Exercise the draft `.fsi` surface from FSI and cap"]:::done
  T010["T010 Record current public surface baseline before impl"]:::done
  T011["T011 Record unsupported-scope and safe-failure expectat"]:::done
  T012["T012 Add semantic test coverage for resolving build/pac"]:::done
  T013["T013 Add rendering smoke coverage that asserts the head"]:::done
  T014["T014 Implement version resolution in Core using install"]:::done
  T015["T015 Wire the resolved version into dashboard snapshot/"]:::done
  T016["T016 Update header rendering so the dashboard name and "]:::done
  T017["T017 Add actionable diagnostics or fallback evidence fo"]:::done
  T018["T018 Capture version-display smoke output in `readiness"]:::done
  T019["T019 Add semantic tests for `rowStripeOdd` and `rowStri"]:::done
  T020["T020 Add renderer tests proving alternating row styles "]:::done
  T021["T021 Add renderer tests proving selected, active, warni"]:::done
  T022["T022 Extend `DashboardColorRole` defaults, parsing, dia"]:::done
  T023["T023 Implement reusable table row style selection so vi"]:::done
  T024["T024 Apply row striping to feature, user story, task, d"]:::done
  T025["T025 Update README and preference examples with stripe "]:::done
  T026["T026 Capture default, configured-color, and invalid-str"]:::done
  T027["T027 Add semantic tests for full-screen command IDs, de"]:::done
  T028["T028 Add state reducer tests for feature/story/plan/tas"]:::done
  T029["T029 Add rendering tests for feature, story, plan, and "]:::done
  T030["T030 Add missing-target tests for unavailable feature, "]:::done
  T031["T031 Add full-screen target and modal state types to th"]:::done
  T032["T032 Extend hotkey commands, default bindings, preferen"]:::done
  T033["T033 Implement app state transitions for opening each f"]:::done
  T034["T034 Load or expose associated source artifact text for"]:::done
  T035["T035 Implement full-screen renderables for feature, sto"]:::done
  T036["T036 Preserve existing navigation, refresh, preference "]:::done
  T037["T037 Update README and quickstart examples for full-scr"]:::done
  T038["T038 Capture feature/story/plan/task full-screen smoke "]:::done
  T039["T039 Refresh surface-area baselines for changed public "]:::done
  T040["T040 Run `dotnet test sk-Dashboard.sln` and capture out"]:::done
  T041["T041 Run scripted dashboard smoke checks for default na"]:::done
  T042["T042 Verify package version bump/install workflow or do"]:::done
  T043["T043 Run `.specify/extensions/evidence/scripts/bash/run"]:::done
  T044["T044 Run the final evidence audit and resolve any `[S]`"]:::done
  T004 --> T005
  T004 --> T006
  T004 --> T007
  T004 --> T008
  T005 --> T009
  T004 --> T009
  T005 --> T010
  T004 --> T010
  T004 --> T011
  T011 --> T012
  T011 --> T013
  T012 --> T014
  T011 --> T014
  T014 --> T015
  T011 --> T015
  T013 --> T016
  T015 --> T016
  T011 --> T016
  T014 --> T017
  T011 --> T017
  T016 --> T018
  T017 --> T018
  T011 --> T018
  T018 --> T019
  T018 --> T020
  T018 --> T021
  T019 --> T022
  T018 --> T022
  T020 --> T023
  T021 --> T023
  T022 --> T023
  T018 --> T023
  T023 --> T024
  T018 --> T024
  T022 --> T025
  T018 --> T025
  T024 --> T026
  T025 --> T026
  T018 --> T026
  T026 --> T027
  T026 --> T028
  T026 --> T029
  T026 --> T030
  T028 --> T031
  T026 --> T031
  T027 --> T032
  T031 --> T032
  T026 --> T032
  T028 --> T033
  T031 --> T033
  T032 --> T033
  T026 --> T033
  T030 --> T034
  T031 --> T034
  T026 --> T034
  T029 --> T035
  T030 --> T035
  T033 --> T035
  T034 --> T035
  T026 --> T035
  T033 --> T036
  T026 --> T036
  T032 --> T037
  T035 --> T037
  T026 --> T037
  T035 --> T038
  T036 --> T038
  T037 --> T038
  T026 --> T038
  T038 --> T039
  T038 --> T040
  T018 --> T041
  T026 --> T041
  T038 --> T041
  T018 --> T042
  T038 --> T042
  T038 --> T043
  T039 --> T044
  T040 --> T044
  T041 --> T044
  T042 --> T044
  T043 --> T044
  T038 --> T044
  classDef pending fill:#eeeeee,stroke:#999
  classDef done fill:#c8e6c9,stroke:#2e7d32
  classDef synthetic fill:#ffe0b2,stroke:#e65100,stroke-width:2px
  classDef autoSynthetic fill:#ffab91,stroke:#bf360c,stroke-width:2px,stroke-dasharray:5 3
  classDef failed fill:#ffcdd2,stroke:#b71c1c,stroke-width:2px
  classDef skipped fill:#f5f5f5,stroke:#666,stroke-dasharray:3 3
```

## ASCII view

```
T001 [X] Confirm active feature metadata points to `specs/003-dashboard-display-controls`
T002 [X] Create `specs/003-dashboard-display-controls/readiness/` for evidence transcripts, smoke output, and graph artifacts
T003 [X] Record Tier 1 public-surface impact for version display, stripe color roles, full-screen commands, and modal state in `readiness/public-surface.md`
T004 [X] Add a display-controls evidence plan covering FSI, semantic tests, render smoke checks, and package version verification
T005 [X] Draft `.fsi` public surface updates for new version metadata, row stripe roles, full-screen target/model types, and dashboard command cases
T006 [X] Add failing semantic tests for command IDs, default full-screen bindings, stripe role parsing/defaults, and version fallback behavior
T007 [X] Add failing dashboard state reducer tests for opening, replacing, and closing full-screen modal targets without changing selected feature/story/task
T008 [X] Add failing rendering smoke tests for header version placement, stripe precedence helpers, and full-screen single-target rendering
T009 [X] Exercise the draft `.fsi` surface from FSI and capture transcript to `readiness/fsi-session.txt`
T010 [X] Record current public surface baseline before implementation in `readiness/public-surface-baseline.txt`
T011 [X] Record unsupported-scope and safe-failure expectations for missing version metadata, missing source text, invalid stripe colors, and unavailable full-screen targets
T012 [X] Add semantic test coverage for resolving build/package version metadata and `vunknown` fallback through the public surface
T013 [X] Add rendering smoke coverage that asserts the header contains `sk-dashboard` plus a version value in wide and narrow layouts
T014 [X] Implement version resolution in Core using installed assembly/package metadata with a stable fallback value
T015 [X] Wire the resolved version into dashboard snapshot/rendering inputs without reading source checkout files at runtime
T016 [X] Update header rendering so the dashboard name and version remain visible in widescreen and vertical layouts
T017 [X] Add actionable diagnostics or fallback evidence for unavailable version metadata
T018 [X] Capture version-display smoke output in `readiness/us1-version-header.txt`
T019 [X] Add semantic tests for `rowStripeOdd` and `rowStripeEven` preference parsing, defaults, invalid values, and low-contrast fallback
T020 [X] Add renderer tests proving alternating row styles apply to feature, story, task, diagnostic, and detail-style tables
T021 [X] Add renderer tests proving selected, active, warning, and error row states override stripe backgrounds
T022 [X] Extend `DashboardColorRole` defaults, parsing, diagnostics, and preference contract support for row stripe roles
T023 [X] Implement reusable table row style selection so visible non-header data rows alternate safely
T024 [X] Apply row striping to feature, user story, task, diagnostic, and detail-like tables without changing selection or ordering behavior
T025 [X] Update README and preference examples with stripe color roles and safe-fallback behavior
T026 [X] Capture default, configured-color, and invalid-stripe smoke outputs in readiness artifacts
T027 [X] Add semantic tests for full-screen command IDs, default bindings, and user-configured binding overrides
T028 [X] Add state reducer tests for feature/story/plan/task modal open, modal replacement, close behavior, and selection preservation
T029 [X] Add rendering tests for feature, story, plan, and task full-screen views showing parsed fields plus source text when available
T030 [X] Add missing-target tests for unavailable feature, story, plan, task, source text, and diagnostics
T031 [X] Add full-screen target and modal state types to the public domain surface
T032 [X] Extend hotkey commands, default bindings, preference parsing, scripted key handling, and footer/help text for four full-screen commands
T033 [X] Implement app state transitions for opening each full-screen target, using selected story/task first and active fallback only when no selected item exists, replacing an open target, and closing without changing selections
T034 [X] Load or expose associated source artifact text for selected feature, story, plan, and task targets with safe unreadable/missing handling
T035 [X] Implement full-screen renderables for feature, story, plan, and task views with exactly one requested target type per modal
T036 [X] Preserve existing navigation, refresh, preference reload, and quit behavior outside full-screen views
T037 [X] Update README and quickstart examples for full-screen hotkeys and scripted smoke checks
T038 [X] Capture feature/story/plan/task full-screen smoke transcripts in readiness artifacts
T039 [X] Refresh surface-area baselines for changed public modules after implementation
T040 [X] Run `dotnet test sk-Dashboard.sln` and capture output to `readiness/dotnet-test.txt`
T041 [X] Run scripted dashboard smoke checks for default navigation plus version, stripe, full-screen flows, table-like full-screen stripe behavior, and lightweight render-cycle/performance evidence
T042 [X] Verify package version bump/install workflow or document why package installation was not performed for this change
T043 [X] Run `.specify/extensions/evidence/scripts/bash/run-audit.sh --graph-only` and capture clean graph output
T044 [X] Run the final evidence audit and resolve any `[S]`, `[S*]`, or diff-scan hits before merge readiness
```

