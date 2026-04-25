#r "../../../src/Core/bin/Debug/net10.0/Core.dll"
#r "../../../src/Dashboard/bin/Debug/net10.0/sk-dashboard.dll"

open SkDashboard.Core
open SkDashboard.Dashboard

let version = Domain.resolveDashboardVersion ()
printfn "version=%s source=%s diagnostic=%b" version.Label version.Source version.Diagnostic.IsSome
printfn "stripeOdd=%s" (Domain.colorRoleId RowStripeOdd)
printfn "stripeEven=%s" (Domain.colorRoleId RowStripeEven)
printfn "fullscreenCommands=%s,%s,%s,%s" (Hotkeys.commandId FullScreenFeature) (Hotkeys.commandId FullScreenStory) (Hotkeys.commandId FullScreenPlan) (Hotkeys.commandId FullScreenTask)

let snapshot = App.load "."
let opened = App.openFullScreen FeatureFullScreen snapshot
printfn "opened=%A selectedFeature=%A" (opened.FullScreen |> Option.map _.Target) opened.SelectedFeatureId
let closed = App.closeFullScreen opened
printfn "closed=%b selections=%A/%A/%A" closed.FullScreen.IsNone closed.SelectedFeatureId closed.SelectedStoryId closed.SelectedTaskId
