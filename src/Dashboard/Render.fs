namespace SkDashboard.Dashboard

open System
open System.IO
open Spectre.Console
open Spectre.Console.Rendering
open SkDashboard.Core

module Render =
    let severityText severity =
        match severity with
        | Info -> "info"
        | Warning -> "warning"
        | Error -> "error"

    let severityColor severity =
        match severity with
        | Info -> Domain.colorRoleId DiagnosticInfo
        | Warning -> Domain.colorRoleId DiagnosticWarning
        | Error -> Domain.colorRoleId DiagnosticError

    let styleFor role (ui: DashboardUiPreferences) =
        ui.Colors
        |> Map.tryFind role
        |> Option.defaultValue (Domain.defaultUiPreferences.Colors[role])

    let styleTag role ui =
        let style = styleFor role ui
        match style.Background with
        | Some background -> style.Foreground + " on " + background
        | None -> style.Foreground

    let markup role ui text =
        sprintf "[%s]%s[/]" (styleTag role ui) (Markup.Escape text)

    let color role ui =
        (styleFor role ui).Foreground

    let artifactText state =
        match state with
        | Missing -> "missing"
        | Present -> "present"
        | Unreadable message -> "unreadable: " + message
        | Malformed message -> "malformed: " + message
        | GraphInvalid message -> "graph-invalid: " + message

    let artifactMarkup state =
        match state with
        | Present -> "[green]present[/]"
        | Missing -> "[grey]missing[/]"
        | Unreadable message -> "[red]unreadable[/] " + Markup.Escape message
        | Malformed message -> "[yellow]malformed[/] " + Markup.Escape message
        | GraphInvalid message -> "[red]invalid[/] " + Markup.Escape message

    let checkoutText state =
        match state with
        | NotAttempted -> "not-attempted"
        | Current -> "current"
        | CheckedOut -> "checked-out"
        | Failed message -> "failed: " + message

    let checkoutMarkup state =
        match state with
        | NotAttempted -> "[grey]not attempted[/]"
        | Current -> "[deepskyblue1]current[/]"
        | CheckedOut -> "[green]checked out[/]"
        | Failed message -> "[red]failed[/] " + Markup.Escape message

    let taskDone (task: SpeckitTask) =
        let status = task.RawStatus.Trim().ToLowerInvariant()
        status = "[x]" || status = "[done]" || status = "[complete]"

    let selectedFeatureRoot (snapshot: DashboardSnapshot) =
        snapshot.SelectedFeatureId
        |> Option.bind (fun selectedId -> snapshot.Features |> List.tryFind (fun feature -> feature.Id = selectedId))
        |> Option.bind _.ArtifactRoot

    let featureTasks snapshot =
        selectedFeatureRoot snapshot
        |> Option.map (fun root -> Path.Combine(root, "tasks.md"))
        |> Option.filter File.Exists
        |> Option.map (fun path ->
            try
                SpeckitArtifacts.parseTasks path |> fst
            with _ ->
                [])
        |> Option.defaultValue []

    let storyProgress tasks storyId =
        let storyTasks = tasks |> List.filter (fun task -> task.RelatedStoryId = Some storyId)
        let total = List.length storyTasks
        let doneCount = storyTasks |> List.filter taskDone |> List.length
        doneCount, total

    let sourceLine (task: SpeckitTask) =
        task.SourceLocation
        |> Option.bind _.Line
        |> Option.defaultValue 0

    let activityTask tasks =
        let storyTasks = tasks |> List.filter (fun task -> task.RelatedStoryId.IsSome)
        let active =
            storyTasks
            |> List.filter (fun task -> task.RawStatus.Trim() <> "[ ]")
            |> List.sortBy sourceLine
            |> List.tryLast

        active
        |> Option.orElse (storyTasks |> List.sortBy sourceLine |> List.tryLast)

    let activityStoryId tasks =
        activityTask tasks |> Option.bind _.RelatedStoryId

    let progressMarkup ui doneCount total =
        if total = 0 then
            sprintf "[%s]          [/][%s] 0/0[/]" (color ProgressIncomplete ui) (color Muted ui)
        else
            let width = 10
            let filled = int (Math.Round(float doneCount / float total * float width))
            let empty = width - filled
            let completeColor =
                if doneCount = total then "green"
                elif doneCount = 0 then color ProgressIncomplete ui
                else "yellow"

            let completeColor =
                if doneCount = total then color ProgressComplete ui else completeColor

            sprintf "[%s]%s[/][%s]%s[/] [bold]%d/%d[/]" completeColor (String('█', filled)) (color ProgressIncomplete ui) (String('░', empty)) doneCount total

    let panel title (renderable: IRenderable) =
        Panel(renderable).Header(PanelHeader(title)).Border(BoxBorder.Rounded)

    let featuresTable snapshot =
        let ui = snapshot.Ui
        let table = Table().NoBorder().Expand()
        table.AddColumn(TableColumn(sprintf "[bold %s]Feature[/]" (color Muted ui))) |> ignore
        table.AddColumn(TableColumn(sprintf "[bold %s]Checkout[/]" (color Muted ui))) |> ignore
        table.AddColumn(TableColumn(sprintf "[bold %s]Artifacts[/]" (color Muted ui))) |> ignore

        match snapshot.Features with
        | [] ->
            table.AddRow(Markup("[yellow]No feature artifacts found[/]"), Markup(""), Markup("[grey]Create specs with Speckit, then press r.[/]")) |> ignore
        | features ->
            for feature in features do
                let name =
                    if feature.IsSelected then
                        sprintf "[%s] %s [/]" (styleTag Selected ui) (Markup.Escape feature.DisplayName)
                    else
                        sprintf "[white]  %s[/]" (Markup.Escape feature.DisplayName)

                let artifacts =
                    match feature.Status with
                    | None -> "[grey]no artifacts[/]"
                    | Some status ->
                        sprintf
                            "S:%s P:%s T:%s C:%s"
                            (artifactMarkup status.SpecState)
                            (artifactMarkup status.PlanState)
                            (artifactMarkup status.TasksState)
                            (artifactMarkup status.ChecklistState)

                table.AddRow(Markup(name), Markup(checkoutMarkup feature.CheckoutState), Markup(artifacts)) |> ignore

        table

    let storiesTable snapshot =
        let ui = snapshot.Ui
        let tasks = featureTasks snapshot
        let lastActivityStoryId = activityStoryId tasks
        let table = Table().NoBorder().Expand()
        table.AddColumn(TableColumn(sprintf "[bold %s]Story[/]" (color Muted ui))) |> ignore
        table.AddColumn(TableColumn(sprintf "[bold %s]Progress[/]" (color Muted ui))) |> ignore

        match snapshot.Stories with
        | [] -> table.AddRow(Markup("[yellow]No user stories[/]"), Markup("")) |> ignore
        | stories ->
            for story in stories do
                let doneCount, total = storyProgress tasks story.Id
                let storyText =
                    if Some story.Id = snapshot.SelectedStoryId then
                        sprintf "[%s] %s [/][white] %s[/]" (styleTag Selected ui) (Markup.Escape story.Id) (Markup.Escape story.Title)
                    elif Some story.Id = lastActivityStoryId then
                        sprintf "[%s] %s [/][%s] %s [/]" (styleTag LastActivity ui) (Markup.Escape story.Id) (styleTag LastActivity ui) (Markup.Escape story.Title)
                    else
                        sprintf "[%s]%s[/] [white]%s[/]" (color PanelAccent ui) (Markup.Escape story.Id) (Markup.Escape story.Title)

                table.AddRow(Markup(storyText), Markup(progressMarkup ui doneCount total)) |> ignore

        table

    let planRenderable snapshot =
        match snapshot.Plan with
        | None -> Markup("[grey]No plan loaded.[/]") :> IRenderable
        | Some plan ->
            let text =
                plan.Summary
                |> Option.orElse (if String.IsNullOrWhiteSpace plan.RawContent then None else Some plan.RawContent)
                |> Option.defaultValue "Plan artifact is empty or missing."

            Markup("[white]" + Markup.Escape text + "[/]") :> IRenderable

    let tasksTable snapshot =
        let ui = snapshot.Ui
        let lastActivityTaskId = featureTasks snapshot |> activityTask |> Option.map _.Id
        let table = Table().NoBorder().Expand()
        table.AddColumn(TableColumn(sprintf "[bold %s]Task[/]" (color Muted ui))) |> ignore
        table.AddColumn(TableColumn(sprintf "[bold %s]Status[/]" (color Muted ui))) |> ignore
        table.AddColumn(TableColumn(sprintf "[bold %s]Title[/]" (color Muted ui))) |> ignore

        match snapshot.TaskGraph with
        | None -> table.AddRow(Markup("[yellow]No task graph[/]"), Markup(""), Markup("")) |> ignore
        | Some graph when List.isEmpty graph.Nodes ->
            table.AddRow(Markup("[yellow]No tasks for selected story[/]"), Markup(""), Markup("")) |> ignore
        | Some graph ->
            graph.Nodes
            |> List.iteri (fun index task ->
                let selected = Some task.Id = graph.SelectedTaskId
                let lastActivity = Some task.Id = lastActivityTaskId
                let rowBackground = if index % 2 = 0 then "black" else "grey7"
                let striped foreground text =
                    sprintf "[%s on %s] %s [/]" foreground rowBackground (Markup.Escape text)

                let id =
                    if selected then
                        sprintf "[%s] %s [/]" (styleTag Selected ui) (Markup.Escape task.Id)
                    elif lastActivity then
                        sprintf "[%s] %s [/]" (styleTag LastActivity ui) (Markup.Escape task.Id)
                    else
                        striped (color PanelAccent ui) task.Id

                let status =
                    if lastActivity then
                        sprintf "[%s] %s [/]" (styleTag LastActivity ui) (Markup.Escape task.RawStatus)
                    elif taskDone task then
                        striped (color ProgressComplete ui) task.RawStatus
                    else
                        striped (color ProgressIncomplete ui) task.RawStatus

                let title =
                    if lastActivity && not selected then
                        sprintf "[%s] %s [/]" (styleTag LastActivity ui) (Markup.Escape task.Title)
                    else
                        striped "white" task.Title

                table.AddRow(Markup(id), Markup(status), Markup(title)) |> ignore
            )

        table

    let detailRenderable snapshot =
        match snapshot.TaskGraph, snapshot.SelectedTaskId with
        | Some graph, Some selectedTaskId ->
            graph.Nodes
            |> List.tryFind (fun task -> task.Id = selectedTaskId)
            |> Option.map (fun task ->
                let deps = if List.isEmpty task.Dependencies then "(none)" else String.concat ", " task.Dependencies
                let source =
                    task.SourceLocation
                    |> Option.map (fun source -> source.Path + ":" + (source.Line |> Option.map string |> Option.defaultValue "?"))
                    |> Option.defaultValue "(unknown)"

                let rows =
                    Rows(
                        [| Markup("[bold white]" + Markup.Escape task.Title + "[/]") :> IRenderable
                           Rule("[grey]details[/]") :> IRenderable
                           Markup(sprintf "[grey]status[/] %s" (Markup.Escape task.RawStatus)) :> IRenderable
                           Markup(sprintf "[grey]story[/] %s" (task.RelatedStoryId |> Option.defaultValue "(none)" |> Markup.Escape)) :> IRenderable
                           Markup(sprintf "[grey]deps[/] %s" (Markup.Escape deps)) :> IRenderable
                           Markup(sprintf "[grey]source[/] %s" (Markup.Escape source)) :> IRenderable
                           Markup("[white]" + Markup.Escape(task.Description |> Option.defaultValue "") + "[/]") :> IRenderable |])

                rows :> IRenderable)
            |> Option.defaultValue (Markup("[grey]No selected task.[/]") :> IRenderable)
        | _ -> Markup("[grey]No selected task.[/]") :> IRenderable

    let diagnosticsRenderable snapshot =
        let ui = snapshot.Ui
        let table = Table().NoBorder().Expand()
        table.AddColumn(TableColumn(sprintf "[bold %s]Severity[/]" (color Muted ui))) |> ignore
        table.AddColumn(TableColumn(sprintf "[bold %s]Message[/]" (color Muted ui))) |> ignore

        match snapshot.Diagnostics with
        | [] -> table.AddRow(Markup("[green]ok[/]"), Markup("[grey]No diagnostics[/]")) |> ignore
        | diagnostics ->
            for diagnostic in diagnostics do
                let role =
                    match diagnostic.Severity with
                    | Info -> DiagnosticInfo
                    | Warning -> DiagnosticWarning
                    | Error -> DiagnosticError

                let message =
                    match diagnostic.Source with
                    | None -> diagnostic.Message
                    | Some source ->
                        let line = source.Line |> Option.map (sprintf ":%d") |> Option.defaultValue ""
                        diagnostic.Message + " (" + source.Path + line + ")"

                table.AddRow(Markup(markup role ui (severityText diagnostic.Severity)), Markup(Markup.Escape message)) |> ignore

        table

    let snapshotText (snapshot: DashboardSnapshot) =
        let featureText =
            match snapshot.Features with
            | [] -> "No feature artifacts found"
            | features ->
                features
                |> List.map (fun feature ->
                    let marker = if feature.IsSelected then "> " else "  "
                    marker + feature.DisplayName + " checkout=" + checkoutText feature.CheckoutState)
                |> String.concat "\n"

        let storyText =
            let tasks = featureTasks snapshot
            let lastActivityStoryId = activityStoryId tasks

            match snapshot.Stories with
            | [] -> "No user stories"
            | stories ->
                stories
                |> List.map (fun story ->
                    let marker = if Some story.Id = snapshot.SelectedStoryId then "> " else "  "
                    let activity = if Some story.Id = lastActivityStoryId then " activity" else ""
                    let doneCount, total = storyProgress tasks story.Id
                    sprintf "%s%s%s %s %d/%d" marker story.Id activity story.Title doneCount total)
                |> String.concat "\n"

        let taskText =
            let lastActivityTaskId = featureTasks snapshot |> activityTask |> Option.map _.Id

            match snapshot.TaskGraph with
            | None -> "No task graph"
            | Some graph ->
                graph.Nodes
                |> List.map (fun task ->
                    let marker = if Some task.Id = graph.SelectedTaskId then "> " else "  "
                    let activity = if Some task.Id = lastActivityTaskId then " activity" else ""
                    sprintf "%s%s%s %s %s" marker task.Id activity task.RawStatus task.Title)
                |> String.concat "\n"

        sprintf
            "Repository: %s\nCurrent branch: %s\nFeatures:\n%s\nStories:\n%s\nTasks:\n%s\nDiagnostics:%d"
            snapshot.RepositoryRoot
            (snapshot.CurrentBranch |> Option.defaultValue "(none)")
            featureText
            storyText
            taskText
            snapshot.Diagnostics.Length

    let snapshotRenderableForWidth width snapshot : IRenderable =
        let ui = snapshot.Ui
        let header =
            let branch = snapshot.CurrentBranch |> Option.defaultValue "(none)" |> Markup.Escape
            Panel(
                Markup(
                    sprintf
                        "[bold %s]sk-dashboard[/]  [%s]repo[/] [white]%s[/]  [%s]branch[/] [yellow]%s[/]"
                        (color PanelAccent ui)
                        (color Muted ui)
                        (Markup.Escape snapshot.RepositoryRoot)
                        (color Muted ui)
                        branch))
                .Border(BoxBorder.None)

        let left = Layout("left").Ratio(3)
        left.SplitRows(
            Layout("features").Ratio(2),
            Layout("stories").Ratio(3))
        |> ignore

        left["features"].Update(panel "[bold deepskyblue1]Features[/]" (featuresTable snapshot)) |> ignore
        left["stories"].Update(panel "[bold green]User Stories[/]" (storiesTable snapshot)) |> ignore

        let right = Layout("right").Ratio(4)
        right.SplitRows(
            Layout("plan").Ratio(2),
            Layout("tasks").Ratio(3),
            Layout("detail").Ratio(2))
        |> ignore

        right["plan"].Update(panel "[bold purple]Plan[/]" (planRenderable snapshot)) |> ignore
        right["tasks"].Update(panel "[bold yellow]Tasks[/]" (tasksTable snapshot)) |> ignore
        right["detail"].Update(panel "[bold white]Task Detail[/]" (detailRenderable snapshot)) |> ignore

        let resolvedLayout = Domain.resolveLayout width snapshot.Ui.Layout

        let main = Layout("main")
        match resolvedLayout with
        | WidescreenLayout -> main.SplitColumns(left, right) |> ignore
        | VerticalLayout ->
            main.SplitRows(
                Layout("features").Ratio(2),
                Layout("stories").Ratio(2),
                Layout("plan").Ratio(2),
                Layout("tasks").Ratio(3),
                Layout("detail").Ratio(2))
            |> ignore

            main["features"].Update(panel (sprintf "[bold %s]Features[/]" (color PanelAccent ui)) (featuresTable snapshot)) |> ignore
            main["stories"].Update(panel "[bold green]User Stories[/]" (storiesTable snapshot)) |> ignore
            main["plan"].Update(panel "[bold purple]Plan[/]" (planRenderable snapshot)) |> ignore
            main["tasks"].Update(panel "[bold yellow]Tasks[/]" (tasksTable snapshot)) |> ignore
            main["detail"].Update(panel "[bold white]Task Detail[/]" (detailRenderable snapshot)) |> ignore

        let footer =
            let actions =
                if List.isEmpty snapshot.Features then
                    "[bold]r[/] refresh  [bold]q[/] quit  [grey]create specs with Speckit to begin[/]"
                else
                    "[bold]j/k[/] features  [bold]up/down[/] stories  [bold]left/right[/] tasks  [bold]r[/] refresh  [bold]enter[/] checkout  [bold]q[/] quit"

            let rows =
                Rows(
                    [| Rule("[grey]controls[/]") :> IRenderable
                       Markup(actions) :> IRenderable
                       diagnosticsRenderable snapshot :> IRenderable |])

            panel "[bold grey]Status[/]" rows

        let root = Layout("root")
        root.SplitRows(
            Layout("header").Size(3),
            Layout("main"),
            Layout("footer").Size(12))
        |> ignore

        root["header"].Update(header) |> ignore
        root["main"].Update(main) |> ignore
        root["footer"].Update(footer) |> ignore
        root :> IRenderable

    let snapshotRenderable snapshot : IRenderable =
        let width =
            try Console.WindowWidth
            with _ -> 120

        snapshotRenderableForWidth width snapshot

    let renderSnapshot snapshot =
        AnsiConsole.Write(snapshotRenderable snapshot)
