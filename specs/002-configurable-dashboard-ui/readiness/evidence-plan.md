# Evidence Plan: Configurable Dashboard UI

## Tier 1 Surface

The feature extends the Core preference-loading surface exposed by `.fsi`
signatures. Evidence must include:

- Updated `src/Core/Hotkeys.fsi` signatures for combined dashboard preferences,
  UI preferences, color roles, layout modes, and validation diagnostics.
- A captured public-surface baseline in `readiness/public-surface.txt`.
- An automated test that verifies the signature-visible preference surface.
- An FSI transcript that loads the compiled Core assembly and exercises the
  public preference-loading entry point.

## Story Evidence

- US1 configurable colors: dashboard smoke evidence using a real preferences
  file with named and hex colors.
- US2 layout options: wide and narrow dashboard smoke evidence, plus keyboard
  navigation evidence across widescreen, vertical, and auto layout modes.
- US3 invalid preferences: dashboard smoke evidence showing invalid values are
  visible diagnostics while startup continues.

## Final Evidence

- Full `dotnet test`.
- Final FSI preference-loading transcript.
- Smoke checks for default, configured-color, widescreen, vertical, auto, and
  invalid-preference scenarios.
- Graph-only audit and full evidence audit captures.
