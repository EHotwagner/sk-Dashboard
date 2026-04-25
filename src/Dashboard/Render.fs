namespace SkDashboard.Dashboard

open Spectre.Console
open SkDashboard.Core

module Render =
    let severityText severity =
        match severity with
        | Info -> "info"
        | Warning -> "warning"
        | Error -> "error"

    let artifactText state =
        match state with
        | Missing -> "missing"
        | Present -> "present"
        | Unreadable message -> "unreadable: " + message
        | Malformed message -> "malformed: " + message
        | GraphInvalid message -> "graph-invalid: " + message

    let checkoutText state =
        match state with
        | NotAttempted -> "not-attempted"
        | Current -> "current"
        | CheckedOut -> "checked-out"
        | Failed message -> "failed: " + message

    let snapshotText (snapshot: DashboardSnapshot) =
        let featureLine (feature: Feature) =
            let marker = if feature.IsSelected then "> " else "  "

            match feature.Status with
            | None -> sprintf "%s%s checkout=%s" marker feature.DisplayName (checkoutText feature.CheckoutState)
            | Some status ->
                sprintf
                    "%s%s checkout=%s spec=%s plan=%s tasks=%s checklist=%s"
                    marker
                    feature.DisplayName
                    (checkoutText feature.CheckoutState)
                    (artifactText status.SpecState)
                    (artifactText status.PlanState)
                    (artifactText status.TasksState)
                    (artifactText status.ChecklistState)

        let featureText =
            match snapshot.Features with
            | [] -> "No feature artifacts found"
            | features -> features |> List.map featureLine |> String.concat "\n"

        let storyText =
            match snapshot.Stories with
            | [] -> "No user stories"
            | stories ->
                stories
                |> List.map (fun story ->
                    let marker = if Some story.Id = snapshot.SelectedStoryId then "> " else "  "
                    marker + story.Id + " " + story.Title)
                |> String.concat "\n"

        let diagnostics =
            snapshot.Diagnostics
            |> List.map (fun d -> sprintf "[%s] %s" (severityText d.Severity) d.Message)
            |> function
                | [] -> "No diagnostics"
                | items -> String.concat "\n" items

        let planText =
            match snapshot.Plan with
            | None -> "No plan"
            | Some plan ->
                plan.Summary
                |> Option.orElse (if System.String.IsNullOrWhiteSpace plan.RawContent then None else Some plan.RawContent)
                |> Option.defaultValue "Plan artifact is empty or missing"

        let taskGraphText =
            match snapshot.TaskGraph with
            | None -> "No task graph"
            | Some graph ->
                let nodes =
                    graph.Nodes
                    |> List.map (fun task ->
                        let marker = if Some task.Id = graph.SelectedTaskId then "> " else "  "
                        sprintf "%s%s %s %s" marker task.Id task.RawStatus task.Title)

                let edges =
                    graph.Edges
                    |> List.map (fun edge -> sprintf "%s -> %s" edge.FromTaskId edge.ToTaskId)

                let graphDiagnostics =
                    graph.Diagnostics
                    |> List.map (fun diagnostic -> sprintf "[%s] %s" (severityText diagnostic.Severity) diagnostic.Message)

                [ "Nodes:" :: nodes
                  "Edges:" :: (if List.isEmpty edges then [ "(none)" ] else edges)
                  "Graph diagnostics:" :: (if List.isEmpty graphDiagnostics then [ "(none)" ] else graphDiagnostics) ]
                |> List.concat
                |> String.concat "\n"

        let detailText =
            match snapshot.TaskGraph, snapshot.SelectedTaskId with
            | Some graph, Some selectedTaskId ->
                graph.Nodes
                |> List.tryFind (fun task -> task.Id = selectedTaskId)
                |> Option.map (fun task ->
                    sprintf
                        "%s\nstatus=%s\nstory=%s\ndeps=%s\nsource=%s\n%s"
                        task.Title
                        task.RawStatus
                        (task.RelatedStoryId |> Option.defaultValue "(none)")
                        (if List.isEmpty task.Dependencies then "(none)" else String.concat ", " task.Dependencies)
                        (task.SourceLocation |> Option.map (fun source -> source.Path + ":" + (source.Line |> Option.map string |> Option.defaultValue "?")) |> Option.defaultValue "(unknown)")
                        (task.Description |> Option.defaultValue ""))
                |> Option.defaultValue "No selected task"
            | _ -> "No selected task"

        let actions =
            if List.isEmpty snapshot.Features then
                "r refresh | q quit | create specs with Speckit to begin"
            else
                "J/K features | j/k stories | h/l tasks | tab panes | r refresh | enter checkout feature | q quit"

        sprintf
            "Repository: %s\nCurrent branch: %s\nFeatures: %s\nStories (left pane):\n%s\nPlan pane:\n%s\nTask graph pane:\n%s\nTask detail pane:\n%s\nActions: %s\nDiagnostics:\n%s"
            snapshot.RepositoryRoot
            (snapshot.CurrentBranch |> Option.defaultValue "(none)")
            featureText
            storyText
            planText
            taskGraphText
            detailText
            actions
            diagnostics

    let renderSnapshot snapshot =
        AnsiConsole.Write(Panel(Markup.Escape(snapshotText snapshot)).Header("sk-dashboard"))
