namespace SkDashboard.Core

module TaskGraphBuilder =
    let build selectedStoryId (tasks: SpeckitTask list) existingDiagnostics =
        let duplicateDiagnostics =
            tasks
            |> List.groupBy _.Id
            |> List.choose (fun (id, items) ->
                if List.length items > 1 then
                    Some(Domain.diagnostic Error ("Duplicate task id: " + id) None)
                else
                    None)

        let taskIds = tasks |> List.map _.Id |> Set.ofList

        let missingDiagnostics =
            tasks
            |> List.collect (fun task ->
                task.Dependencies
                |> List.choose (fun dep ->
                    if Set.contains dep taskIds then
                        None
                    else
                        Some(Domain.diagnostic Error (sprintf "Task %s references missing dependency %s." task.Id dep) task.SourceLocation)))

        let cycleDiagnostics =
            let tasksById = tasks |> List.map (fun task -> task.Id, task) |> Map.ofList

            let rec visit path taskId =
                if List.contains taskId path then
                    [ Domain.diagnostic Error ("Task dependency cycle detected: " + (List.rev (taskId :: path) |> String.concat " -> ")) None ]
                else
                    match Map.tryFind taskId tasksById with
                    | None -> []
                    | Some task -> task.Dependencies |> List.collect (visit (taskId :: path))

            tasks
            |> List.collect (fun task -> visit [] task.Id)
            |> List.distinct

        let scoped =
            match selectedStoryId with
            | None -> tasks
            | Some storyId ->
                let selected = tasks |> List.filter (fun task -> task.RelatedStoryId = Some storyId)
                let selectedIds = selected |> List.map _.Id |> Set.ofList
                let tasksById = tasks |> List.map (fun task -> task.Id, task) |> Map.ofList

                let rec collectDependencies seen pending =
                    match pending with
                    | [] -> seen
                    | dep :: rest when Set.contains dep seen -> collectDependencies seen rest
                    | dep :: rest ->
                        let seen' = Set.add dep seen

                        match Map.tryFind dep tasksById with
                        | Some task -> collectDependencies seen' (task.Dependencies @ rest)
                        | None -> collectDependencies seen' rest

                let dependencyIds =
                    selected
                    |> List.collect _.Dependencies
                    |> collectDependencies Set.empty

                tasks
                |> List.filter (fun task -> Set.contains task.Id selectedIds || Set.contains task.Id dependencyIds)

        let scopedIds = scoped |> List.map _.Id |> Set.ofList

        let edges =
            scoped
            |> List.collect (fun task ->
                task.Dependencies
                |> List.choose (fun dep ->
                    if Set.contains dep scopedIds then
                        Some { FromTaskId = dep; ToTaskId = task.Id }
                    else
                        None))

        { SelectedStoryId = selectedStoryId
          Nodes = scoped
          Edges = edges
          Diagnostics = existingDiagnostics @ duplicateDiagnostics @ missingDiagnostics @ cycleDiagnostics
          SelectedTaskId = scoped |> List.tryHead |> Option.map _.Id }
