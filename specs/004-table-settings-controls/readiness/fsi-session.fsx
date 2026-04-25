#r "../../../src/Core/bin/Debug/net10.0/Core.dll"
#r "../../../src/Dashboard/bin/Debug/net10.0/sk-dashboard.dll"

open SkDashboard.Core
open SkDashboard.Dashboard

let tableViewport =
    Domain.defaultTableViewport 5 20
    |> Domain.keepRowVisible 499 500

printfn "table viewport rowOffset=%d visibleRows=%d" tableViewport.RowOffset tableViewport.VisibleRows

let detailViewport =
    Domain.defaultDetailViewport 10 80
    |> Domain.scrollDetailLines 1990 2000

printfn "detail viewport lineOffset=%d visibleLines=%d" detailViewport.LineOffset detailViewport.VisibleLines

let settings =
    { Domain.defaultUiPreferences with
        Table = { Domain.defaultUiPreferences.Table with Border = HeavyBorder }
        Detail = { Domain.defaultUiPreferences.Detail with HorizontalStep = 12 } }

let version =
    { Path = "dashboard.json"
      LastWriteTimeUtc = None
      Length = None }

let session = Domain.settingsSession settings version
printfn "settings dirty=%b border=%s" session.Dirty (Domain.tableBorderId session.Draft.Table.Border)

let snapshot =
    App.load "."
    |> App.applyCommand "." SettingsOpen

printfn "settings target=%A diagnostics=%d" (snapshot.FullScreen |> Option.map _.Target) snapshot.Diagnostics.Length
