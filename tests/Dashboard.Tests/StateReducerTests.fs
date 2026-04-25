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

        test "commandForKey_maps feature and story navigation" {
            Expect.equal (Input.commandForKey "K") (Some FeaturePrevious) "Feature previous is reachable."
            Expect.equal (Input.commandForKey "J") (Some FeatureNext) "Feature next is reachable."
            Expect.equal (Input.commandForKey "k") (Some StoryPrevious) "Story previous is reachable."
            Expect.equal (Input.commandForKey "j") (Some StoryNext) "Story next is reachable."
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
                  Diagnostics = []
                  LastRefreshedAt = DateTimeOffset.UnixEpoch }

            let next = App.selectTask 1 snapshot
            Expect.equal next.SelectedTaskId (Some "T002") "Next task is selected."
            Expect.equal (next.TaskGraph |> Option.bind _.SelectedTaskId) (Some "T002") "Graph selected task follows."
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
