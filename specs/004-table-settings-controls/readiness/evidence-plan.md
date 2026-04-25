# Public Surface Baseline

Changed Tier 1 surfaces:
- src/Core/Domain.fsi: table/detail viewport state, display preferences, table border style, settings edit sessions, config version, live reload state.
- src/Core/Hotkeys.fsi: settings commands, table/detail scroll commands, config version/write helpers.
- src/Dashboard/Program.fs CLI: --settings mode over the shared dashboard config path.

Real evidence paths:
- dotnet test sk-Dashboard.sln
- dotnet fsi readiness/fsi-session.fsx against built assemblies
- dotnet run --project src/Dashboard -- --keys scripted dashboard commands
- dotnet run --project src/Dashboard -- --settings --config <temp-path>
