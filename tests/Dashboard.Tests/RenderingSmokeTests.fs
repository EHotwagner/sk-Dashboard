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

        test "snapshotText_marks_last_story_activity" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-render-activity-").FullName
            let featureRoot = Path.Combine(root, "specs", "001-a")
            Directory.CreateDirectory(featureRoot) |> ignore
            File.WriteAllText(
                Path.Combine(featureRoot, "spec.md"),
                "### User Story 1 - First (Priority: P1)\n\n### User Story 2 - Second (Priority: P1)\n")
            File.WriteAllText(
                Path.Combine(featureRoot, "tasks.md"),
                "- [X] T001 [US1] Done first\n- [X] T002 [US2] Done second\n")

            let text = App.load root |> App.selectStory 1 |> Render.snapshotText
            Expect.stringContains text "US2 activity" "Last story-scoped task marks its story as active."
            Expect.stringContains text "T002 activity" "Last story-scoped task is marked active."
        }
    ]
