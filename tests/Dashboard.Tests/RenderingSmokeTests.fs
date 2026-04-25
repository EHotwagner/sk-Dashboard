module SkDashboard.Dashboard.Tests.RenderingSmokeTests

open System.IO
open Expecto
open SkDashboard.Dashboard

[<Tests>]
let renderingSmokeTests =
    testList "Rendering" [
        test "snapshotText_handles empty project state" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-render-").FullName
            let text = App.load root |> Render.snapshotText
            Expect.stringContains text "No feature artifacts found" "Empty state is visible."
        }
    ]
