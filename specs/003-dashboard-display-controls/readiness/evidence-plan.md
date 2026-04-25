# Evidence Plan

- FSI public surface exercise: load built Core and Dashboard assemblies, resolve version metadata, inspect stripe roles, and open/close modal state.
- Semantic tests: Expecto coverage for hotkey command IDs, preference parsing, row stripe roles, version fallback shape, and reducer modal transitions.
- Rendering smoke: `Render.snapshotText`, `Render.snapshotRenderableForWidth`, and scripted `dotnet run --keys` checks for version, stripes, and full-screen commands.
- Package version evidence: use assembly/package metadata from the built dashboard executable; document local package install status before merge.

