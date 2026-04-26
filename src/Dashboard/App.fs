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

    let mergeBranchFeatures
        (snapshot: DashboardSnapshot)
        (branchFeatures: Feature list)
        (selectedBranch: Feature option)
        checkoutState
        =
        let byId =
            snapshot.Features |> List.map (fun feature -> feature.Id, feature) |> Map.ofList

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

        let features =
            mergeBranchFeatures snapshot branchFeatures selectedBranch checkoutState

        let selectedFeatureId =
            selectedBranch |> Option.map _.Id |> Option.orElse snapshot.SelectedFeatureId

        let currentBranch =
            GitFeatures.currentBranch root |> Option.orElse snapshot.CurrentBranch

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

        let selectedBranch =
            branchFeatures
            |> List.tryFind (fun feature -> Some feature.Id = selectedFeatureId)

        let snapshot = SpeckitArtifacts.loadSnapshot root

        let features =
            mergeBranchFeatures snapshot branchFeatures selectedBranch checkoutState

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

    let load projectPath = loadWithAutoCheckout true projectPath

    let refresh projectPath = load projectPath

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
        let selectedId =
            moveSelection offset snapshot.SelectedFeatureId (fun (feature: Feature) -> feature.Id) snapshot.Features

        let features =
            snapshot.Features
            |> List.map (fun feature ->
                { feature with
                    IsSelected = Some feature.Id = selectedId })

        { snapshot with
            Features = features
            SelectedFeatureId = selectedId }

    let selectedFeatureRoot (snapshot: DashboardSnapshot) =
        snapshot.SelectedFeatureId
        |> Option.bind (fun selectedId -> snapshot.Features |> List.tryFind (fun feature -> feature.Id = selectedId))
        |> Option.bind _.ArtifactRoot

    let rebuildTaskGraphForStory selectedStoryId (snapshot: DashboardSnapshot) =
        match selectedFeatureRoot snapshot with
        | None ->
            { snapshot with
                SelectedStoryId = selectedStoryId
                TaskGraph = None
                SelectedTaskId = None }
        | Some featureRoot ->
            let tasks, diagnostics =
                SpeckitArtifacts.parseTasks (Path.Combine(featureRoot, "tasks.md"))

            let graph = TaskGraphBuilder.build selectedStoryId tasks diagnostics

            { snapshot with
                SelectedStoryId = selectedStoryId
                TaskGraph = Some graph
                SelectedTaskId = graph.SelectedTaskId }

    let selectStory offset (snapshot: DashboardSnapshot) =
        let selectedStoryId =
            moveSelection offset snapshot.SelectedStoryId (fun (story: UserStory) -> story.Id) snapshot.Stories

        rebuildTaskGraphForStory selectedStoryId snapshot

    let selectTask offset (snapshot: DashboardSnapshot) =
        match snapshot.TaskGraph with
        | None -> snapshot
        | Some graph ->
            let selectedTaskId =
                moveSelection offset snapshot.SelectedTaskId (fun (task: SpeckitTask) -> task.Id) graph.Nodes

            { snapshot with
                SelectedTaskId = selectedTaskId
                TaskGraph =
                    Some
                        { graph with
                            SelectedTaskId = selectedTaskId } }

    let keepSelectedId previousId getId items fallbackId =
        previousId
        |> Option.filter (fun selected -> items |> List.exists (fun item -> getId item = selected))
        |> Option.orElse fallbackId

    let preserveSelections (previous: DashboardSnapshot) (next: DashboardSnapshot) =
        let selectedFeatureId =
            keepSelectedId
                previous.SelectedFeatureId
                (fun (feature: Feature) -> feature.Id)
                next.Features
                next.SelectedFeatureId

        let selectedStoryId =
            keepSelectedId
                previous.SelectedStoryId
                (fun (story: UserStory) -> story.Id)
                next.Stories
                next.SelectedStoryId

        let features =
            next.Features
            |> List.map (fun feature ->
                { feature with
                    IsSelected = Some feature.Id = selectedFeatureId })

        let nextWithSelections =
            { next with
                Features = features
                SelectedFeatureId = selectedFeatureId
                SelectedStoryId = selectedStoryId }
            |> rebuildTaskGraphForStory selectedStoryId

        let selectedTaskId =
            match nextWithSelections.TaskGraph with
            | None -> None
            | Some graph ->
                keepSelectedId
                    previous.SelectedTaskId
                    (fun (task: SpeckitTask) -> task.Id)
                    graph.Nodes
                    nextWithSelections.SelectedTaskId

        let taskGraph =
            nextWithSelections.TaskGraph
            |> Option.map (fun graph ->
                { graph with
                    SelectedTaskId = selectedTaskId })

        { nextWithSelections with
            SelectedTaskId = selectedTaskId
            TaskGraph = taskGraph
            FullScreen = previous.FullScreen }

    let checkoutSelectedFeature projectPath snapshot =
        let selected =
            snapshot.SelectedFeatureId
            |> Option.bind (fun selectedId ->
                snapshot.Features |> List.tryFind (fun feature -> feature.Id = selectedId))

        match selected |> Option.bind _.BranchName with
        | None -> snapshot
        | Some branchName ->
            let checkoutState =
                GitFeatures.checkoutBranch (SpeckitArtifacts.resolveRepositoryRoot projectPath) branchName

            loadWithSelectedBranch projectPath (selected |> Option.map _.Id) checkoutState

    let openFullScreen target snapshot =
        { snapshot with
            FullScreen =
                Some
                    { Target = target
                      SelectedFeatureId = snapshot.SelectedFeatureId
                      SelectedStoryId = snapshot.SelectedStoryId
                      SelectedTaskId = snapshot.SelectedTaskId
                      Document = None
                      Checklist = None
                      Viewport = Domain.defaultDetailViewport 40 120 } }

    let constitutionPath projectPath =
        Path.Combine(SpeckitArtifacts.resolveRepositoryRoot projectPath, Domain.constitutionRelativePath)

    let constitutionDocument projectPath =
        let path = constitutionPath projectPath

        let source =
            Some
                { Path = path
                  DisplayName = "constitution.md" }

        try
            if not (File.Exists path) then
                { Title = "Constitution"
                  RawContent = "Constitution is unavailable: " + path
                  Source = source
                  Status = MarkdownSourceMissing
                  Diagnostics =
                    [ { Message = "Constitution file is missing."
                        SourcePath = Some path } ] }
            else
                let text = File.ReadAllText path

                if String.IsNullOrWhiteSpace text then
                    { Title = "Constitution"
                      RawContent = "Constitution file is empty: " + path
                      Source = source
                      Status = MarkdownEmptyDocument
                      Diagnostics =
                        [ { Message = "Constitution file is empty."
                            SourcePath = Some path } ] }
                else
                    { Title = "Constitution"
                      RawContent = text
                      Source = source
                      Status = MarkdownRendered
                      Diagnostics = [] }
        with ex ->
            { Title = "Constitution"
              RawContent = "Constitution is unreadable: " + path + "\n" + ex.Message
              Source = source
              Status = MarkdownSourceUnreadable ex.Message
              Diagnostics =
                [ { Message = "Constitution file is unreadable: " + ex.Message
                    SourcePath = Some path } ] }

    let openConstitution projectPath snapshot =
        let document = constitutionDocument projectPath

        let diagnostics =
            document.Diagnostics
            |> List.map (fun diagnostic ->
                Domain.diagnostic
                    Warning
                    diagnostic.Message
                    (diagnostic.SourcePath |> Option.map (fun path -> { Path = path; Line = None })))

        { snapshot with
            Diagnostics = snapshot.Diagnostics @ diagnostics
            FullScreen =
                Some
                    { Target = ConstitutionFullScreen
                      SelectedFeatureId = snapshot.SelectedFeatureId
                      SelectedStoryId = snapshot.SelectedStoryId
                      SelectedTaskId = snapshot.SelectedTaskId
                      Document = Some document
                      Checklist = None
                      Viewport = Domain.defaultDetailViewport 40 120 } }

    let dashboardContext snapshot =
        { SelectedFeatureId = snapshot.SelectedFeatureId
          SelectedStoryId = snapshot.SelectedStoryId
          SelectedTaskId = snapshot.SelectedTaskId
          FocusedPaneId = snapshot.Panes |> List.tryFind _.IsFocused |> Option.map _.Id
          PreviousFullScreenTarget = snapshot.FullScreen |> Option.map _.Target }

    let checklistDiagnostics (document: MarkdownDocument) =
        document.Diagnostics
        |> List.map (fun diagnostic ->
            Domain.diagnostic
                Warning
                diagnostic.Message
                (diagnostic.SourcePath |> Option.map (fun path -> { Path = path; Line = None })))

    let checklistListDocument (checklists: ChecklistArtifact list) (diagnostics: Diagnostic list) =
        let body =
            match checklists with
            | [] ->
                "# Checklists\n\nNo checklists are available for the active feature."
            | values ->
                values
                |> List.mapi (fun index checklist ->
                    let marker = if index = 0 then ">" else "-"
                    sprintf "%s %s" marker checklist.DisplayName)
                |> String.concat "\n"
                |> sprintf "# Checklists\n\n%s\n\nPress enter to open the selected checklist."

        { Title = "Checklists"
          RawContent = body
          Source = None
          Status = if List.isEmpty checklists then MarkdownEmptyDocument else MarkdownRendered
          Diagnostics =
            diagnostics
            |> List.map (fun diagnostic ->
                { Message = diagnostic.Message
                  SourcePath = diagnostic.Source |> Option.map _.Path }) }

    let openChecklists _projectPath snapshot =
        match selectedFeatureRoot snapshot with
        | None ->
            let diagnostic = Domain.diagnostic Info "No active feature is selected for checklist discovery." None
            let state =
                { AvailableChecklists = []
                  SelectedChecklist = None
                  Document = Some(checklistListDocument [] [ diagnostic ])
                  PreviousContext = dashboardContext snapshot
                  Viewport = Domain.defaultDetailViewport 40 120
                  Diagnostics = [ diagnostic ]
                  Mode = ChecklistEmpty }

            { snapshot with
                Diagnostics = snapshot.Diagnostics @ [ diagnostic ]
                FullScreen =
                    Some
                        { Target = ChecklistFullScreen
                          SelectedFeatureId = snapshot.SelectedFeatureId
                          SelectedStoryId = snapshot.SelectedStoryId
                          SelectedTaskId = snapshot.SelectedTaskId
                          Document = state.Document
                          Checklist = Some state
                          Viewport = state.Viewport } }
        | Some featureRoot ->
            let checklists, diagnostics = SpeckitArtifacts.discoverChecklists featureRoot
            let mode = if List.isEmpty checklists then ChecklistEmpty else ChecklistListing
            let state =
                { AvailableChecklists = checklists
                  SelectedChecklist = checklists |> List.tryHead
                  Document = Some(checklistListDocument checklists diagnostics)
                  PreviousContext = dashboardContext snapshot
                  Viewport = Domain.defaultDetailViewport 40 120
                  Diagnostics = diagnostics
                  Mode = mode }

            { snapshot with
                Diagnostics = snapshot.Diagnostics @ diagnostics
                FullScreen =
                    Some
                        { Target = ChecklistFullScreen
                          SelectedFeatureId = snapshot.SelectedFeatureId
                          SelectedStoryId = snapshot.SelectedStoryId
                          SelectedTaskId = snapshot.SelectedTaskId
                          Document = state.Document
                          Checklist = Some state
                          Viewport = state.Viewport } }

    let openSelectedChecklist snapshot =
        match snapshot.FullScreen with
        | Some modal when modal.Target = ChecklistFullScreen ->
            match modal.Checklist |> Option.bind _.SelectedChecklist with
            | None -> snapshot
            | Some checklist ->
                let document = SpeckitArtifacts.loadChecklistDocument checklist
                let diagnostics = checklistDiagnostics document
                let nextState =
                    modal.Checklist
                    |> Option.map (fun state ->
                        { state with
                            Document = Some document
                            Diagnostics = state.Diagnostics @ diagnostics
                            Mode = ChecklistReading })

                { snapshot with
                    Diagnostics = snapshot.Diagnostics @ diagnostics
                    FullScreen =
                        Some
                            { modal with
                                Document = Some document
                                Checklist = nextState } }
        | _ -> snapshot

    let closeFullScreen snapshot = { snapshot with FullScreen = None }

    let scrollTableColumns delta snapshot =
        let panes =
            snapshot.Panes
            |> List.map (fun pane ->
                match pane.Kind with
                | Features
                | Stories
                | TaskGraph
                | Diagnostics ->
                    { pane with
                        ScrollOffset = max 0 (pane.ScrollOffset + delta) }
                | _ -> pane)

        { snapshot with Panes = panes }

    let fullScreenContentMetrics snapshot modal =
        let text = Render.fullScreenText snapshot modal
        let lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
        let widest = lines |> Array.map _.Length |> Array.append [| 0 |] |> Array.max
        lines.Length, widest

    let scrollFullScreenLines delta snapshot =
        match snapshot.FullScreen with
        | None -> snapshot
        | Some modal ->
            let lineCount, columnCount = fullScreenContentMetrics snapshot modal

            let viewport =
                Domain.clampDetailViewport
                    lineCount
                    columnCount
                    { modal.Viewport with
                        LineOffset = modal.Viewport.LineOffset + delta }

            { snapshot with
                FullScreen = Some { modal with Viewport = viewport } }

    let scrollFullScreenColumns delta snapshot =
        match snapshot.FullScreen with
        | None -> snapshot
        | Some modal ->
            let lineCount, columnCount = fullScreenContentMetrics snapshot modal

            let viewport =
                Domain.clampDetailViewport
                    lineCount
                    columnCount
                    { modal.Viewport with
                        ColumnOffset = modal.Viewport.ColumnOffset + delta }

            { snapshot with
                FullScreen = Some { modal with Viewport = viewport } }

    let settingsDiagnostic (snapshot: DashboardSnapshot) =
        let diagnostic =
            Domain.diagnostic
                Info
                "Settings page opened. Use sk-dashboard --settings to edit the shared dashboard config in a separate console."
                None

        { snapshot with
            Diagnostics = snapshot.Diagnostics @ [ diagnostic ] }

    let borderChoices = [ NoBorder; MinimalBorder; RoundedBorder; HeavyBorder ]

    let cycleBorder delta snapshot =
        let currentIndex =
            borderChoices
            |> List.tryFindIndex ((=) snapshot.Ui.Table.Border)
            |> Option.defaultValue 0

        let nextIndex =
            (currentIndex + delta + List.length borderChoices) % List.length borderChoices

        let nextBorder = borderChoices |> List.item nextIndex

        { snapshot with
            Ui =
                { snapshot.Ui with
                    Table =
                        { snapshot.Ui.Table with
                            Border = nextBorder } } }

    let adjustDetailStep delta snapshot =
        let nextStep = max 1 (snapshot.Ui.Detail.HorizontalStep + delta)

        { snapshot with
            Ui =
                { snapshot.Ui with
                    Detail =
                        { snapshot.Ui.Detail with
                            HorizontalStep = nextStep } } }

    let cycleValue delta current choices =
        match choices with
        | [] -> current
        | values ->
            let currentIndex =
                values
                |> List.tryFindIndex ((=) current)
                |> Option.defaultValue 0

            let nextIndex = (currentIndex + delta + List.length values) % List.length values
            values |> List.item nextIndex

    let cycleAppTheme delta snapshot =
        let next =
            cycleValue delta snapshot.Ui.Themes.SelectedAppThemeId snapshot.Ui.Themes.AvailableAppThemes

        { snapshot with
            Ui =
                { snapshot.Ui with
                    Table = Domain.defaultUiPreferences.Table
                    Colors = Domain.defaultUiPreferences.Colors
                    Themes =
                        { snapshot.Ui.Themes with
                            SelectedAppThemeId = next } } }

    let cycleMarkdownTheme delta snapshot =
        let next =
            cycleValue delta snapshot.Ui.Themes.SelectedMarkdownThemeId snapshot.Ui.Themes.AvailableMarkdownThemes

        { snapshot with
            Ui =
                { snapshot.Ui with
                    Themes =
                        { snapshot.Ui.Themes with
                            SelectedMarkdownThemeId = next } } }

    let settingsSurfaceActive snapshot =
        snapshot.FullScreen
        |> Option.exists (fun modal -> modal.Target = SettingsFullScreen)

    let settingsFocus snapshot =
        snapshot.FullScreen
        |> Option.map _.Viewport.LineOffset
        |> Option.defaultValue 0
        |> max 0
        |> min 3

    let setSettingsFocus focus snapshot =
        match snapshot.FullScreen with
        | Some modal when modal.Target = SettingsFullScreen ->
            let viewport =
                { modal.Viewport with
                    LineOffset = focus |> max 0 |> min 3 }

            { snapshot with
                FullScreen = Some { modal with Viewport = viewport } }
        | _ -> snapshot

    let moveSettingsFocus delta snapshot =
        setSettingsFocus ((settingsFocus snapshot + delta + 4) % 4) snapshot

    let changeFocusedSetting delta snapshot =
        match settingsFocus snapshot with
        | 0 -> cycleAppTheme delta snapshot
        | 1 -> cycleMarkdownTheme delta snapshot
        | 2 -> cycleBorder delta snapshot
        | _ -> adjustDetailStep delta snapshot

    let fullScreenActive snapshot = snapshot.FullScreen.IsSome

    let applyCommand projectPath command snapshot =
        match command with
        | StoryPrevious when settingsSurfaceActive snapshot -> moveSettingsFocus -1 snapshot
        | StoryNext when settingsSurfaceActive snapshot -> moveSettingsFocus 1 snapshot
        | TaskPrevious when settingsSurfaceActive snapshot -> changeFocusedSetting -1 snapshot
        | TaskNext when settingsSurfaceActive snapshot -> changeFocusedSetting 1 snapshot
        | StoryPrevious when fullScreenActive snapshot -> scrollFullScreenLines -5 snapshot
        | StoryNext when fullScreenActive snapshot -> scrollFullScreenLines 5 snapshot
        | TaskPrevious when fullScreenActive snapshot ->
            scrollFullScreenColumns -snapshot.Ui.Detail.HorizontalStep snapshot
        | TaskNext when fullScreenActive snapshot -> scrollFullScreenColumns snapshot.Ui.Detail.HorizontalStep snapshot
        | FeatureCheckout when snapshot.FullScreen |> Option.exists (fun modal -> modal.Target = ChecklistFullScreen) ->
            openSelectedChecklist snapshot
        | FeaturePrevious -> selectFeature -1 snapshot
        | FeatureNext -> selectFeature 1 snapshot
        | StoryPrevious -> selectStory -1 snapshot
        | StoryNext -> selectStory 1 snapshot
        | TaskPrevious -> selectTask -1 snapshot
        | TaskNext -> selectTask 1 snapshot
        | FeatureCheckout -> checkoutSelectedFeature projectPath snapshot
        | DetailsClose -> closeFullScreen snapshot
        | FullScreenFeature -> openFullScreen FeatureFullScreen snapshot
        | FullScreenStory -> openFullScreen StoryFullScreen snapshot
        | FullScreenPlan -> openFullScreen PlanFullScreen snapshot
        | FullScreenTask -> openFullScreen TaskFullScreen snapshot
        | ConstitutionOpen -> openConstitution projectPath snapshot
        | ChecklistOpen -> openChecklists projectPath snapshot
        | SettingsAppThemePrevious when settingsSurfaceActive snapshot -> cycleAppTheme -1 snapshot
        | SettingsAppThemeNext when settingsSurfaceActive snapshot -> cycleAppTheme 1 snapshot
        | SettingsMarkdownThemePrevious when settingsSurfaceActive snapshot -> cycleMarkdownTheme -1 snapshot
        | SettingsMarkdownThemeNext when settingsSurfaceActive snapshot -> cycleMarkdownTheme 1 snapshot
        | TableScrollLeft when settingsSurfaceActive snapshot -> cycleBorder -1 snapshot
        | TableScrollRight when settingsSurfaceActive snapshot -> cycleBorder 1 snapshot
        | TableScrollLeft -> scrollTableColumns -snapshot.Ui.Table.HorizontalStep snapshot
        | TableScrollRight -> scrollTableColumns snapshot.Ui.Table.HorizontalStep snapshot
        | DetailScrollLeft when settingsSurfaceActive snapshot -> adjustDetailStep -1 snapshot
        | DetailScrollRight when settingsSurfaceActive snapshot -> adjustDetailStep 1 snapshot
        | DetailScrollUp -> scrollFullScreenLines -5 snapshot
        | DetailScrollDown -> scrollFullScreenLines 5 snapshot
        | DetailScrollLeft -> scrollFullScreenColumns -snapshot.Ui.Detail.HorizontalStep snapshot
        | DetailScrollRight -> scrollFullScreenColumns snapshot.Ui.Detail.HorizontalStep snapshot
        | SettingsOpen -> openFullScreen SettingsFullScreen (settingsDiagnostic snapshot)
        | Refresh -> loadWithAutoCheckout false projectPath
        | _ -> snapshot

    let initialRefreshModel projectPath =
        { ProjectPath = projectPath
          Pending = false
          QueuedTriggers = []
          Snapshot = None }

    let enqueueRefresh trigger model =
        let triggers = trigger :: model.QueuedTriggers |> List.distinct |> List.sort

        { model with
            Pending = true
            QueuedTriggers = triggers }

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

        let watchPath =
            if Directory.Exists specsPath then
                specsPath
            else
                projectPath

        let watcher = new FileSystemWatcher(watchPath)
        watcher.IncludeSubdirectories <- true
        watcher.EnableRaisingEvents <- true
        watcher.Changed.Add(fun _ -> drain FileChanged)
        watcher.Created.Add(fun _ -> drain FileChanged)
        watcher.Deleted.Add(fun _ -> drain FileChanged)
        watcher.Renamed.Add(fun _ -> drain FileChanged)

        let timer =
            new Timer(TimerCallback(fun _ -> drain Poll), null, refreshInterval, refreshInterval)

        { new IDisposable with
            member _.Dispose() =
                timer.Dispose()
                watcher.Dispose() }
