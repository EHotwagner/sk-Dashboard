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
        | Info -> "deepskyblue1"
        | Warning -> "yellow"
        | Error -> "red"

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

    let progressMarkup doneCount total =
        if total = 0 then
            "[grey]          [/][grey] 0/0[/]"
        else
            let width = 10
            let filled = int (Math.Round(float doneCount / float total * float width))
            let empty = width - filled
            let color =
                if doneCount = total then "green"
                elif doneCount = 0 then "grey"
                else "yellow"

            sprintf "[%s]%s[/][grey]%s[/] [bold]%d/%d[/]" color (String('█', filled)) (String('░', empty)) doneCount total

    let panel title (renderable: IRenderable) =
        Panel(renderable).Header(PanelHeader(title)).Border(BoxBorder.Rounded)

    let featuresTable snapshot =
        let table = Table().NoBorder().Expand()
        table.AddColumn(TableColumn("[bold grey]Feature[/]")) |> ignore
        table.AddColumn(TableColumn("[bold grey]Checkout[/]")) |> ignore
        table.AddColumn(TableColumn("[bold grey]Artifacts[/]")) |> ignore

        match snapshot.Features with
        | [] ->
            table.AddRow(Markup("[yellow]No feature artifacts found[/]"), Markup(""), Markup("[grey]Create specs with Speckit, then press r.[/]")) |> ignore
        | features ->
            for feature in features do
                let name =
                    if feature.IsSelected then
                        sprintf "[black on deepskyblue1] %s [/]" (Markup.Escape feature.DisplayName)
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
        let tasks = featureTasks snapshot
        let lastActivityStoryId = activityStoryId tasks
        let table = Table().NoBorder().Expand()
        table.AddColumn(TableColumn("[bold grey]Story[/]")) |> ignore
        table.AddColumn(TableColumn("[bold grey]Progress[/]")) |> ignore

        match snapshot.Stories with
        | [] -> table.AddRow(Markup("[yellow]No user stories[/]"), Markup("")) |> ignore
        | stories ->
            for story in stories do
                let doneCount, total = storyProgress tasks story.Id
                let storyText =
                    if Some story.Id = snapshot.SelectedStoryId then
                        sprintf "[black on green] %s [/][white] %s[/]" (Markup.Escape story.Id) (Markup.Escape story.Title)
                    elif Some story.Id = lastActivityStoryId then
                        sprintf "[black on grey42] %s [/][white on grey23] %s [/]" (Markup.Escape story.Id) (Markup.Escape story.Title)
                    else
                        sprintf "[deepskyblue1]%s[/] [white]%s[/]" (Markup.Escape story.Id) (Markup.Escape story.Title)

                table.AddRow(Markup(storyText), Markup(progressMarkup doneCount total)) |> ignore

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
        let lastActivityTaskId = featureTasks snapshot |> activityTask |> Option.map _.Id
        let table = Table().NoBorder().Expand()
        table.AddColumn(TableColumn("[bold grey]Task[/]")) |> ignore
        table.AddColumn(TableColumn("[bold grey]Status[/]")) |> ignore
        table.AddColumn(TableColumn("[bold grey]Title[/]")) |> ignore

        match snapshot.TaskGraph with
        | None -> table.AddRow(Markup("[yellow]No task graph[/]"), Markup(""), Markup("")) |> ignore
        | Some graph when List.isEmpty graph.Nodes ->
            table.AddRow(Markup("[yellow]No tasks for selected story[/]"), Markup(""), Markup("")) |> ignore
        | Some graph ->
            for task in graph.Nodes do
                let selected = Some task.Id = graph.SelectedTaskId
                let lastActivity = Some task.Id = lastActivityTaskId
                let id =
                    if selected then
                        sprintf "[black on yellow] %s [/]" (Markup.Escape task.Id)
                    elif lastActivity then
                        sprintf "[black on grey42] %s [/]" (Markup.Escape task.Id)
                    else
                        sprintf "[deepskyblue1]%s[/]" (Markup.Escape task.Id)

                let status =
                    if lastActivity then
                        "[black on grey42] " + Markup.Escape task.RawStatus + " [/]"
                    elif taskDone task then
                        "[green]" + Markup.Escape task.RawStatus + "[/]"
                    else
                        "[grey]" + Markup.Escape task.RawStatus + "[/]"

                let title =
                    if lastActivity && not selected then
                        "[white on grey23] " + Markup.Escape task.Title + " [/]"
                    else
                        Markup.Escape task.Title

                table.AddRow(Markup(id), Markup(status), Markup(title)) |> ignore

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
        let table = Table().NoBorder().Expand()
        table.AddColumn(TableColumn("[bold grey]Severity[/]")) |> ignore
        table.AddColumn(TableColumn("[bold grey]Message[/]")) |> ignore

        match snapshot.Diagnostics with
        | [] -> table.AddRow(Markup("[green]ok[/]"), Markup("[grey]No diagnostics[/]")) |> ignore
        | diagnostics ->
            for diagnostic in diagnostics do
                let color = severityColor diagnostic.Severity
                table.AddRow(Markup(sprintf "[%s]%s[/]" color (severityText diagnostic.Severity)), Markup(Markup.Escape diagnostic.Message)) |> ignore

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

    let snapshotRenderable snapshot : IRenderable =
        let header =
            let branch = snapshot.CurrentBranch |> Option.defaultValue "(none)" |> Markup.Escape
            Panel(
                Markup(
                    sprintf
                        "[bold deepskyblue1]sk-dashboard[/]  [grey]repo[/] [white]%s[/]  [grey]branch[/] [yellow]%s[/]"
                        (Markup.Escape snapshot.RepositoryRoot)
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

        let main = Layout("main")
        main.SplitColumns(left, right) |> ignore

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
            Layout("footer").Size(7))
        |> ignore

        root["header"].Update(header) |> ignore
        root["main"].Update(main) |> ignore
        root["footer"].Update(footer) |> ignore
        root :> IRenderable

    let renderSnapshot snapshot =
        AnsiConsole.Write(snapshotRenderable snapshot)
