namespace SkDashboard.Dashboard

open System
open System.IO
open System.Threading
open SkDashboard.Core

type RefreshTrigger =
    | Manual
    | FileChanged
    | Poll

type RefreshModel =
    { ProjectPath: string
      Pending: bool
      QueuedTriggers: RefreshTrigger list
      Snapshot: DashboardSnapshot option }

module App =
    let checkoutDiagnostic branchName state =
        match state with
        | Failed message -> [ Domain.diagnostic Error (sprintf "Checkout of %s failed: %s" branchName message) None ]
        | _ -> []

    let mergeBranchFeatures (snapshot: DashboardSnapshot) (branchFeatures: Feature list) (selectedBranch: Feature option) checkoutState =
        let byId =
            snapshot.Features
            |> List.map (fun feature -> feature.Id, feature)
            |> Map.ofList

        let branchRows =
            branchFeatures
            |> List.map (fun branchFeature ->
                let checkout =
                    match selectedBranch with
                    | Some selected when selected.Id = branchFeature.Id -> checkoutState
                    | _ -> branchFeature.CheckoutState

                match Map.tryFind branchFeature.Id byId with
                | Some artifactFeature ->
                    { artifactFeature with
                        BranchName = branchFeature.BranchName
                        CheckoutState = checkout
                        IsSelected = Some branchFeature.Id = (selectedBranch |> Option.map _.Id) }
                | None ->
                    { branchFeature with
                        CheckoutState = checkout
                        IsSelected = Some branchFeature.Id = (selectedBranch |> Option.map _.Id) })

        let artifactOnly =
            snapshot.Features
            |> List.filter (fun feature -> branchFeatures |> List.exists (fun branch -> branch.Id = feature.Id) |> not)

        branchRows @ artifactOnly

    let loadWithAutoCheckout autoCheckout projectPath =
        let root = SpeckitArtifacts.resolveRepositoryRoot projectPath
        let branchFeatures = GitFeatures.listFeatureBranches root
        let selectedBranch = GitFeatures.selectLatestFeature branchFeatures

        let checkoutState =
            match autoCheckout, selectedBranch |> Option.bind _.BranchName with
            | true, Some branchName -> GitFeatures.checkoutBranch root branchName
            | false, Some branchName when GitFeatures.currentBranch root = Some branchName -> Current
            | _ -> NotAttempted

        let snapshot = SpeckitArtifacts.loadSnapshot root
        let features = mergeBranchFeatures snapshot branchFeatures selectedBranch checkoutState
        let selectedFeatureId = selectedBranch |> Option.map _.Id |> Option.orElse snapshot.SelectedFeatureId
        let currentBranch = GitFeatures.currentBranch root |> Option.orElse snapshot.CurrentBranch

        let checkoutDiagnostics =
            match selectedBranch |> Option.bind _.BranchName with
            | Some branchName -> checkoutDiagnostic branchName checkoutState
            | None -> []

        { snapshot with
            CurrentBranch = currentBranch
            Features = features
            SelectedFeatureId = selectedFeatureId
            Diagnostics = snapshot.Diagnostics @ checkoutDiagnostics }

    let loadWithSelectedBranch projectPath selectedFeatureId checkoutState =
        let root = SpeckitArtifacts.resolveRepositoryRoot projectPath
        let branchFeatures = GitFeatures.listFeatureBranches root
        let selectedBranch = branchFeatures |> List.tryFind (fun feature -> Some feature.Id = selectedFeatureId)
        let snapshot = SpeckitArtifacts.loadSnapshot root
        let features = mergeBranchFeatures snapshot branchFeatures selectedBranch checkoutState

        { snapshot with
            CurrentBranch = GitFeatures.currentBranch root |> Option.orElse snapshot.CurrentBranch
            Features = features
            SelectedFeatureId = selectedFeatureId
            Diagnostics =
                snapshot.Diagnostics
                @ (selectedBranch
                   |> Option.bind _.BranchName
                   |> Option.map (fun branchName -> checkoutDiagnostic branchName checkoutState)
                   |> Option.defaultValue []) }

    let load projectPath =
        loadWithAutoCheckout true projectPath

    let refresh projectPath =
        load projectPath

    let moveSelection offset selectedId getId items =
        match items with
        | [] -> None
        | _ ->
            let currentIndex =
                selectedId
                |> Option.bind (fun selected -> items |> List.tryFindIndex (fun item -> getId item = selected))
                |> Option.defaultValue 0

            let nextIndex = (currentIndex + offset + List.length items) % List.length items
            items |> List.item nextIndex |> getId |> Some

    let selectFeature offset (snapshot: DashboardSnapshot) =
        let selectedId = moveSelection offset snapshot.SelectedFeatureId (fun (feature: Feature) -> feature.Id) snapshot.Features

        let features =
            snapshot.Features
            |> List.map (fun feature -> { feature with IsSelected = Some feature.Id = selectedId })

        { snapshot with Features = features; SelectedFeatureId = selectedId }

    let selectStory offset (snapshot: DashboardSnapshot) =
        { snapshot with SelectedStoryId = moveSelection offset snapshot.SelectedStoryId (fun (story: UserStory) -> story.Id) snapshot.Stories }

    let selectTask offset (snapshot: DashboardSnapshot) =
        match snapshot.TaskGraph with
        | None -> snapshot
        | Some graph ->
            let selectedTaskId = moveSelection offset snapshot.SelectedTaskId (fun (task: SpeckitTask) -> task.Id) graph.Nodes
            { snapshot with
                SelectedTaskId = selectedTaskId
                TaskGraph = Some { graph with SelectedTaskId = selectedTaskId } }

    let checkoutSelectedFeature projectPath snapshot =
        let selected =
            snapshot.SelectedFeatureId
            |> Option.bind (fun selectedId -> snapshot.Features |> List.tryFind (fun feature -> feature.Id = selectedId))

        match selected |> Option.bind _.BranchName with
        | None -> snapshot
        | Some branchName ->
            let checkoutState = GitFeatures.checkoutBranch (SpeckitArtifacts.resolveRepositoryRoot projectPath) branchName
            loadWithSelectedBranch projectPath (selected |> Option.map _.Id) checkoutState

    let applyCommand projectPath command snapshot =
        match command with
        | FeaturePrevious -> selectFeature -1 snapshot
        | FeatureNext -> selectFeature 1 snapshot
        | StoryPrevious -> selectStory -1 snapshot
        | StoryNext -> selectStory 1 snapshot
        | TaskPrevious -> selectTask -1 snapshot
        | TaskNext -> selectTask 1 snapshot
        | FeatureCheckout -> checkoutSelectedFeature projectPath snapshot
        | Refresh -> loadWithAutoCheckout false projectPath
        | _ -> snapshot

    let initialRefreshModel projectPath =
        { ProjectPath = projectPath
          Pending = false
          QueuedTriggers = []
          Snapshot = None }

    let enqueueRefresh trigger model =
        let triggers =
            trigger :: model.QueuedTriggers
            |> List.distinct
            |> List.sort

        { model with Pending = true; QueuedTriggers = triggers }

    let drainRefresh model =
        if not model.Pending then
            model, None
        else
            let snapshot = refresh model.ProjectPath

            { model with
                Pending = false
                QueuedTriggers = []
                Snapshot = Some snapshot },
            Some snapshot

    let startRefreshOrchestration projectPath (refreshInterval: TimeSpan) onSnapshot onDiagnostic =
        let modelLock = obj ()
        let mutable model = initialRefreshModel projectPath

        let drain trigger =
            try
                let snapshot =
                    lock modelLock (fun () ->
                        model <- enqueueRefresh trigger model
                        let next, snapshot = drainRefresh model
                        model <- next
                        snapshot)

                snapshot |> Option.iter onSnapshot
            with ex ->
                Domain.diagnostic Error ("Refresh failed: " + ex.Message) None |> onDiagnostic

        let specsPath = Path.Combine(projectPath, "specs")
        let watchPath = if Directory.Exists specsPath then specsPath else projectPath
        let watcher = new FileSystemWatcher(watchPath)
        watcher.IncludeSubdirectories <- true
        watcher.EnableRaisingEvents <- true
        watcher.Changed.Add(fun _ -> drain FileChanged)
        watcher.Created.Add(fun _ -> drain FileChanged)
        watcher.Deleted.Add(fun _ -> drain FileChanged)
        watcher.Renamed.Add(fun _ -> drain FileChanged)

        let timer =
            new Timer(
                TimerCallback(fun _ -> drain Poll),
                null,
                refreshInterval,
                refreshInterval)

        { new IDisposable with
            member _.Dispose() =
                timer.Dispose()
                watcher.Dispose() }
