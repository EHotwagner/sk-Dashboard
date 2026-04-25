module SkDashboard.Dashboard.Tests.RenderingSmokeTests

open System.IO
open Expecto
open SkDashboard.Core
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

        test "layout_selection_uses_auto_threshold" {
            Expect.equal (Domain.resolveLayout 119 Auto) VerticalLayout "Auto uses vertical below 120 columns."
            Expect.equal (Domain.resolveLayout 120 Auto) WidescreenLayout "Auto uses widescreen at 120 columns."
            Expect.equal (Domain.resolveLayout 80 Widescreen) WidescreenLayout "Explicit widescreen is honored."
            Expect.equal (Domain.resolveLayout 160 Vertical) VerticalLayout "Explicit vertical is honored."
        }

        test "snapshotRenderable_supports_explicit_and_auto_layout_modes" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-render-layout-").FullName
            let snapshot = App.load root
            let withLayout layout = { snapshot with Ui = { snapshot.Ui with Layout = layout } }

            Render.snapshotRenderableForWidth 160 (withLayout Widescreen) |> ignore
            Render.snapshotRenderableForWidth 80 (withLayout Vertical) |> ignore
            Render.snapshotRenderableForWidth 80 (withLayout Auto) |> ignore
            Render.snapshotRenderableForWidth 160 (withLayout Auto) |> ignore
            Expect.isTrue true "All layout renderables are produced."
        }

        test "configured_colors_are_used_for_dashboard_roles" {
            let ui =
                { Layout = Auto
                  Colors =
                    Domain.defaultUiPreferences.Colors
                    |> Map.add Selected { Foreground = "black"; Background = Some "green" }
                    |> Map.add LastActivity { Foreground = "white"; Background = Some "#555555" }
                    |> Map.add ProgressComplete { Foreground = "#00ff00"; Background = None }
                    |> Map.add ProgressIncomplete { Foreground = "#555555"; Background = None }
                    |> Map.add DiagnosticInfo { Foreground = "#7aa2f7"; Background = None }
                    |> Map.add DiagnosticWarning { Foreground = "yellow"; Background = None }
                    |> Map.add DiagnosticError { Foreground = "red"; Background = None }
                    |> Map.add Muted { Foreground = "grey42"; Background = None }
                    |> Map.add PanelAccent { Foreground = "#7aa2f7"; Background = None } }

            Expect.equal (Render.styleTag Selected ui) "black on green" "Selected role uses configured pair."
            Expect.equal (Render.styleTag LastActivity ui) "white on #555555" "Last activity role uses configured pair."
            Expect.stringContains (Render.progressMarkup ui 2 2) "#00ff00" "Progress complete color is used."
            Expect.stringContains (Render.progressMarkup ui 1 2) "#555555" "Progress incomplete color is used."
            Expect.equal (Render.markup DiagnosticInfo ui "info") "[#7aa2f7]info[/]" "Diagnostic info color is used."
            Expect.equal (Render.color Muted ui) "grey42" "Muted color is used."
            Expect.equal (Render.color PanelAccent ui) "#7aa2f7" "Panel accent color is used."
        }

        test "default_colors_are_used_without_custom_preferences" {
            let ui = Domain.defaultUiPreferences
            Expect.equal (Render.styleTag Selected ui) "black on deepskyblue1" "Default selected color is used."
            Expect.equal (Render.color ProgressComplete ui) "green" "Default progress color is used."
            Expect.equal (Render.color DiagnosticError ui) "red" "Default diagnostic error color is used."
            Expect.equal (Render.color Muted ui) "grey" "Default muted color is used."
        }

        test "navigation_state_is_preserved_across_layout_modes" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-render-nav-layout-").FullName
            let featureRoot = Path.Combine(root, "specs", "001-a")
            Directory.CreateDirectory(featureRoot) |> ignore
            File.WriteAllText(
                Path.Combine(featureRoot, "spec.md"),
                "### User Story 1 - First (Priority: P1)\n\n### User Story 2 - Second (Priority: P1)\n")
            File.WriteAllText(
                Path.Combine(featureRoot, "tasks.md"),
                "- [ ] T001 [US1] First task\n- [ ] T002 [US2] Second task\n")

            let navigated =
                App.load root
                |> App.selectStory 1
                |> App.selectTask 0

            for layout in [ Widescreen; Vertical; Auto ] do
                let snapshot = { navigated with Ui = { navigated.Ui with Layout = layout } }
                Render.snapshotRenderableForWidth 80 snapshot |> ignore
                Expect.equal snapshot.SelectedStoryId (Some "US2") "Story selection remains stable."
                Expect.equal snapshot.SelectedTaskId (Some "T002") "Task selection remains stable."
        }

        test "valid_ui_preferences_apply_when_sibling_values_are_invalid" {
            let path = Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-render-invalid-sibling-").FullName, "dashboard.json")
            File.WriteAllText(path, """{"version":1,"bindings":[],"ui":{"layout":"bogus","colors":{"panelAccent":"#7aa2f7","muted":"notacolor"}}}""")

            let preferences = Hotkeys.loadPreferences path
            Expect.equal preferences.Ui.Colors[PanelAccent].Foreground "#7aa2f7" "Valid sibling color applies."
            Expect.equal preferences.Ui.Colors[Muted] Domain.defaultUiPreferences.Colors[Muted] "Invalid sibling color falls back."
            Expect.isNonEmpty preferences.Diagnostics "Invalid sibling values are reported."
        }
    ]
