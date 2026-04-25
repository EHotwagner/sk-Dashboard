module SkDashboard.Dashboard.Tests.StateReducerTests

open System
open System.IO
open System.Threading
open Expecto
open SkDashboard.Core
open SkDashboard.Dashboard

[<Tests>]
let stateReducerTests =
    testList "Input" [
        test "commandForKey_maps refresh and quit" {
            Expect.isSome (Input.commandForKey "r") "Refresh is reachable."
            Expect.isSome (Input.commandForKey "q") "Quit is reachable."
        }

        test "keySequenceFromConsoleKey_normalizes interactive keys" {
            Expect.equal (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false))) "q" "Printable keys are preserved."
            Expect.equal (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('\000', ConsoleKey.Enter, false, false, false))) "enter" "Enter is named."
            Expect.equal (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('\t', ConsoleKey.Tab, true, false, false))) "shift+tab" "Shift-tab is named."
            Expect.equal (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('\000', ConsoleKey.DownArrow, false, false, false))) "down" "Down arrow is named."
            Expect.equal (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('\000', ConsoleKey.RightArrow, true, false, false))) "shift+right" "Shift-right is named."
        }

        test "commandForKey_maps feature and story navigation" {
            Expect.equal (Input.commandForKey "k") (Some FeaturePrevious) "Feature previous is reachable."
            Expect.equal (Input.commandForKey "j") (Some FeatureNext) "Feature next is reachable."
            Expect.equal (Input.commandForKey "up") (Some StoryPrevious) "Story previous is reachable."
            Expect.equal (Input.commandForKey "down") (Some StoryNext) "Story next is reachable."
            Expect.equal (Input.commandForKey "left") (Some TaskPrevious) "Task previous is reachable."
            Expect.equal (Input.commandForKey "right") (Some TaskNext) "Task next is reachable."
        }

        test "commandForKeyWithBindings_applies valid override" {
            let bindings =
                Hotkeys.defaultBindings
                |> List.map (fun binding ->
                    if binding.Command = StoryNext then
                        { binding with KeySequence = "n"; Source = "test" }
                    else
                        binding)

            Expect.equal (Input.commandForKeyWithBindings bindings "n") (Some StoryNext) "Custom story key is routed."
        }

        test "selectFeature_updates selected feature state" {
            let status = Domain.emptyFeatureStatus DateTimeOffset.UnixEpoch
            let feature id =
                { Id = id
                  BranchName = Some id
                  DisplayName = id
                  OrderKey = Fallback id
                  IsSelected = id = "001-a"
                  ArtifactRoot = None
                  CheckoutState = NotAttempted
                  Status = Some status }

            let snapshot =
                { RepositoryRoot = "."
                  CurrentBranch = None
                  Features = [ feature "001-a"; feature "002-b" ]
                  SelectedFeatureId = Some "001-a"
                  Stories = []
                  SelectedStoryId = None
                  Plan = None
                  TaskGraph = None
                  SelectedTaskId = None
                  Panes = Domain.defaultPanes
                  Ui = Domain.defaultUiPreferences
                  Diagnostics = []
                  LastRefreshedAt = DateTimeOffset.UnixEpoch }

            let next = App.selectFeature 1 snapshot
            Expect.equal next.SelectedFeatureId (Some "002-b") "Next feature is selected."
            Expect.equal (next.Features |> List.map _.IsSelected) [ false; true ] "Feature selection flags update."
        }

        test "selectStory_updates selected story state" {
            let story id =
                { Id = id
                  Title = id
                  Priority = None
                  Description = ""
                  AcceptanceScenarios = []
                  SourceLocation = None }

            let snapshot =
                { RepositoryRoot = "."
                  CurrentBranch = None
                  Features = []
                  SelectedFeatureId = None
                  Stories = [ story "US1"; story "US2" ]
                  SelectedStoryId = Some "US1"
                  Plan = None
                  TaskGraph = None
                  SelectedTaskId = None
                  Panes = Domain.defaultPanes
                  Ui = Domain.defaultUiPreferences
                  Diagnostics = []
                  LastRefreshedAt = DateTimeOffset.UnixEpoch }

            let next = App.selectStory 1 snapshot
            Expect.equal next.SelectedStoryId (Some "US2") "Next story is selected."
        }

        test "applyCommand_routes feature and story navigation" {
            let story id =
                { Id = id
                  Title = id
                  Priority = None
                  Description = ""
                  AcceptanceScenarios = []
                  SourceLocation = None }

            let feature id =
                { Id = id
                  BranchName = Some id
                  DisplayName = id
                  OrderKey = Fallback id
                  IsSelected = id = "001-a"
                  ArtifactRoot = None
                  CheckoutState = NotAttempted
                  Status = None }

            let snapshot =
                { RepositoryRoot = "."
                  CurrentBranch = None
                  Features = [ feature "001-a"; feature "002-b" ]
                  SelectedFeatureId = Some "001-a"
                  Stories = [ story "US1"; story "US2" ]
                  SelectedStoryId = Some "US1"
                  Plan = None
                  TaskGraph = None
                  SelectedTaskId = None
                  Panes = Domain.defaultPanes
                  Ui = Domain.defaultUiPreferences
                  Diagnostics = []
                  LastRefreshedAt = DateTimeOffset.UnixEpoch }

            let next =
                snapshot
                |> App.applyCommand "." FeatureNext
                |> App.applyCommand "." StoryNext

            Expect.equal next.SelectedFeatureId (Some "002-b") "FeatureNext is applied."
            Expect.equal next.SelectedStoryId (Some "US2") "StoryNext is applied."
        }

        test "selectTask_updates selected task detail state" {
            let task id =
                { Id = id
                  Title = id
                  Description = None
                  RawStatus = "[ ]"
                  Dependencies = []
                  RelatedStoryId = Some "US1"
                  SourceLocation = None
                  Metadata = Map.empty }

            let graph =
                { SelectedStoryId = Some "US1"
                  Nodes = [ task "T001"; task "T002" ]
                  Edges = []
                  Diagnostics = []
                  SelectedTaskId = Some "T001" }

            let snapshot =
                { RepositoryRoot = "."
                  CurrentBranch = None
                  Features = []
                  SelectedFeatureId = None
                  Stories = []
                  SelectedStoryId = Some "US1"
                  Plan = None
                  TaskGraph = Some graph
                  SelectedTaskId = Some "T001"
                  Panes = Domain.defaultPanes
                  Ui = Domain.defaultUiPreferences
                  Diagnostics = []
                  LastRefreshedAt = DateTimeOffset.UnixEpoch }

            let next = App.selectTask 1 snapshot
            Expect.equal next.SelectedTaskId (Some "T002") "Next task is selected."
            Expect.equal (next.TaskGraph |> Option.bind _.SelectedTaskId) (Some "T002") "Graph selected task follows."
        }

        test "selectStory_rebuilds_task_graph_for_selected_story" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-story-").FullName
            let featureRoot = Path.Combine(root, "specs", "001-a")
            Directory.CreateDirectory(featureRoot) |> ignore
            File.WriteAllText(
                Path.Combine(featureRoot, "tasks.md"),
                "- [ ] T001 [US1] First story task\n- [ ] T002 [US2] Second story task\n")

            let feature =
                { Id = "001-a"
                  BranchName = Some "001-a"
                  DisplayName = "001-a"
                  OrderKey = Fallback "001-a"
                  IsSelected = true
                  ArtifactRoot = Some featureRoot
                  CheckoutState = NotAttempted
                  Status = None }

            let story id =
                { Id = id
                  Title = id
                  Priority = None
                  Description = ""
                  AcceptanceScenarios = []
                  SourceLocation = None }

            let graph =
                { SelectedStoryId = Some "US1"
                  Nodes =
                    [ { Id = "T001"
                        Title = "[US1] First story task"
                        Description = None
                        RawStatus = "[ ]"
                        Dependencies = []
                        RelatedStoryId = Some "US1"
                        SourceLocation = None
                        Metadata = Map.empty } ]
                  Edges = []
                  Diagnostics = []
                  SelectedTaskId = Some "T001" }

            let snapshot =
                { RepositoryRoot = root
                  CurrentBranch = None
                  Features = [ feature ]
                  SelectedFeatureId = Some "001-a"
                  Stories = [ story "US1"; story "US2" ]
                  SelectedStoryId = Some "US1"
                  Plan = None
                  TaskGraph = Some graph
                  SelectedTaskId = Some "T001"
                  Panes = Domain.defaultPanes
                  Ui = Domain.defaultUiPreferences
                  Diagnostics = []
                  LastRefreshedAt = DateTimeOffset.UnixEpoch }

            let next = App.selectStory 1 snapshot
            Expect.equal next.SelectedStoryId (Some "US2") "Story selection moved."
            Expect.equal (next.TaskGraph |> Option.map _.Nodes |> Option.defaultValue [] |> List.map _.Id) [ "T002" ] "Task graph was rebuilt for the new story."
            Expect.equal next.SelectedTaskId (Some "T002") "Task selection follows rebuilt graph."
        }

        test "preserveSelections_keeps_visible_selection_across_refresh" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-preserve-").FullName
            let featureRoot id =
                let path = Path.Combine(root, "specs", id)
                Directory.CreateDirectory(path) |> ignore
                File.WriteAllText(
                    Path.Combine(path, "tasks.md"),
                    "- [ ] T001 [US1] First task\n- [ ] T002 [US2] Second task\n")
                path

            let feature id selected =
                { Id = id
                  BranchName = Some id
                  DisplayName = id
                  OrderKey = Fallback id
                  IsSelected = selected
                  ArtifactRoot = Some(featureRoot id)
                  CheckoutState = NotAttempted
                  Status = None }

            let story id =
                { Id = id
                  Title = id
                  Priority = None
                  Description = ""
                  AcceptanceScenarios = []
                  SourceLocation = None }

            let task id =
                { Id = id
                  Title = id
                  Description = None
                  RawStatus = "[ ]"
                  Dependencies = []
                  RelatedStoryId = Some "US1"
                  SourceLocation = None
                  Metadata = Map.empty }

            let snapshot selectedFeature selectedStory selectedTask =
                let graph =
                    { SelectedStoryId = Some "US1"
                      Nodes = [ task "T001"; task "T002" ]
                      Edges = []
                      Diagnostics = []
                      SelectedTaskId = selectedTask }

                { RepositoryRoot = "."
                  CurrentBranch = None
                  Features = [ feature "001-a" (selectedFeature = Some "001-a"); feature "002-b" (selectedFeature = Some "002-b") ]
                  SelectedFeatureId = selectedFeature
                  Stories = [ story "US1"; story "US2" ]
                  SelectedStoryId = selectedStory
                  Plan = None
                  TaskGraph = Some graph
                  SelectedTaskId = selectedTask
                  Panes = Domain.defaultPanes
                  Ui = Domain.defaultUiPreferences
                  Diagnostics = []
                  LastRefreshedAt = DateTimeOffset.UnixEpoch }

            let previous = snapshot (Some "002-b") (Some "US2") (Some "T002")
            let refreshed = snapshot (Some "001-a") (Some "US1") (Some "T001")
            let next = App.preserveSelections previous refreshed

            Expect.equal next.SelectedFeatureId (Some "002-b") "Feature selection is preserved."
            Expect.equal next.SelectedStoryId (Some "US2") "Story selection is preserved."
            Expect.equal next.SelectedTaskId (Some "T002") "Task selection is preserved."
            Expect.equal (next.Features |> List.map _.IsSelected) [ false; true ] "Feature selection flags follow preserved state."
        }

        test "refresh events coalesce into one pending snapshot load" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-refresh-").FullName
            let model =
                App.initialRefreshModel root
                |> App.enqueueRefresh Manual
                |> App.enqueueRefresh FileChanged
                |> App.enqueueRefresh FileChanged

            Expect.isTrue model.Pending "Refresh is pending."
            Expect.equal model.QueuedTriggers [ Manual; FileChanged ] "Duplicate refresh reasons are coalesced."

            let next, snapshot = App.drainRefresh model
            Expect.isFalse next.Pending "Refresh is drained."
            Expect.isSome snapshot "Refresh produces a new snapshot."
        }

        test "refresh orchestration observes polling and file changes" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-refresh-watch-").FullName
            use observed = new ManualResetEventSlim(false)
            let mutable count = 0

            use handle =
                App.startRefreshOrchestration
                    root
                    (TimeSpan.FromMilliseconds 50.0)
                    (fun _ ->
                        count <- count + 1
                        if count >= 2 then
                            observed.Set())
                    (fun _ -> observed.Set())

            Directory.CreateDirectory(Path.Combine(root, "specs")) |> ignore
            File.WriteAllText(Path.Combine(root, "specs", "touch.txt"), "changed")

            Expect.isTrue (observed.Wait(TimeSpan.FromSeconds 3.0)) "Polling and file-system refreshes are observed."
            Expect.isGreaterThanOrEqual count 2 "At least two refresh callbacks were received."
        }
    ]
