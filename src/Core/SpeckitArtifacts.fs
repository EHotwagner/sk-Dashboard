namespace SkDashboard.Core

open System
open System.IO
open System.Text.RegularExpressions

module SpeckitArtifacts =
  let resolveRepositoryRoot path =
    Path.GetFullPath(if String.IsNullOrWhiteSpace path then "." else path)

  let resolveUserConfigPath () =
    let home: string =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

    let baseDir: string =
        match Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") with
        | null -> if not (String.IsNullOrWhiteSpace home) then Path.Combine(home, ".config") else Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        | value when not (String.IsNullOrWhiteSpace value) -> value
        | _ when not (String.IsNullOrWhiteSpace home) -> Path.Combine(home, ".config")
        | _ -> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

    Path.Combine(baseDir, "sk-dashboard", "hotkeys.json")

  let artifactState path =
    if File.Exists path then
        try
            File.ReadAllText path |> ignore
            Present
        with ex ->
            Unreadable ex.Message
    else
        Missing

  let artifactStateWith validator path =
    if File.Exists path then
        try
            let text = File.ReadAllText path
            match validator text with
            | Some message -> Malformed message
            | None -> Present
        with ex ->
            Unreadable ex.Message
    else
        Missing

  let featureFromDirectory (directory: DirectoryInfo) =
    let name = directory.Name
    let id = name
    let orderKey =
        let first = name.Split('-', StringSplitOptions.RemoveEmptyEntries) |> Array.tryHead

        match first with
        | Some value ->
            match Int32.TryParse value with
            | true, number -> Numeric number
            | false, _ when Regex.IsMatch(value, "^\d{8,14}$") -> Timestamp value
            | _ -> Fallback name
        | None -> Fallback name

    { Id = id
      BranchName = None
      DisplayName = name
      OrderKey = orderKey
      IsSelected = false
      ArtifactRoot = Some directory.FullName
      CheckoutState = NotAttempted
      Status = None }

  let discoverFeatureDirectories repositoryRoot =
    let specs = Path.Combine(repositoryRoot, "specs")

    if not (Directory.Exists specs) then
        []
    else
        Directory.EnumerateDirectories specs
        |> Seq.map (DirectoryInfo >> featureFromDirectory)
        |> Seq.sortBy (fun feature -> feature.DisplayName)
        |> Seq.toList

  let summarizeFeatureStatus featureRoot =
    let now = DateTimeOffset.UtcNow
    let checklistRoot = Path.Combine(featureRoot, "checklists")
    let specState =
        artifactStateWith
            (fun text ->
                if String.IsNullOrWhiteSpace text then Some "spec.md is empty"
                elif not (text.Contains("User Story")) then Some "spec.md has no user story headings"
                else None)
            (Path.Combine(featureRoot, "spec.md"))

    let planState =
        artifactStateWith
            (fun text ->
                if String.IsNullOrWhiteSpace text then Some "plan.md is empty"
                elif not (text.Contains("## Summary")) then Some "plan.md has no Summary section"
                else None)
            (Path.Combine(featureRoot, "plan.md"))

    let tasksState =
        artifactStateWith
            (fun text ->
                if String.IsNullOrWhiteSpace text then Some "tasks.md is empty"
                elif not (Regex.IsMatch(text, @"(?m)^- \[.*?\]\s+T\d+")) then Some "tasks.md has no task rows"
                else None)
            (Path.Combine(featureRoot, "tasks.md"))

    let checklistState =
        if Directory.Exists checklistRoot
           && (Directory.EnumerateFiles(checklistRoot, "*.md") |> Seq.isEmpty |> not) then
            Present
        else
            Missing

    let diagnostics =
        [ "spec.md", specState
          "plan.md", planState
          "tasks.md", tasksState ]
        |> List.choose (fun (name, state) ->
            match state with
            | Missing -> Some(Domain.diagnostic Info (name + " is missing.") (Some { Path = Path.Combine(featureRoot, name); Line = None }))
            | Unreadable message -> Some(Domain.diagnostic Error (name + " is unreadable: " + message) (Some { Path = Path.Combine(featureRoot, name); Line = None }))
            | Malformed message -> Some(Domain.diagnostic Warning (name + " is malformed: " + message) (Some { Path = Path.Combine(featureRoot, name); Line = None }))
            | GraphInvalid message -> Some(Domain.diagnostic Error (name + " graph is invalid: " + message) (Some { Path = Path.Combine(featureRoot, name); Line = None }))
            | Present -> None)

    { SpecState = specState
      PlanState = planState
      TasksState = tasksState
      ChecklistState = checklistState
      Diagnostics = diagnostics
      LastRefreshedAt = now }

  let sectionBetween heading (text: string) =
    let pattern = sprintf @"(?ms)^## %s\s*(.*?)(?=^## |\z)" (Regex.Escape heading)
    let m = Regex.Match(text, pattern)
    if m.Success then Some(m.Groups[1].Value.Trim()) else None

  let loadPlan featureRoot =
    let path = Path.Combine(featureRoot, "plan.md")

    if not (File.Exists path) then
        { Path = None
          Summary = None
          TechnicalContext = None
          ConstitutionCheck = None
          RawContent = ""
          Diagnostics = [ Domain.diagnostic Info "Plan artifact is missing." None ] }
    else
        try
            let text = File.ReadAllText path
            { Path = Some path
              Summary = sectionBetween "Summary" text
              TechnicalContext = sectionBetween "Technical Context" text
              ConstitutionCheck = sectionBetween "Constitution Check" text
              RawContent = text
              Diagnostics = [] }
        with ex ->
            { Path = Some path
              Summary = None
              TechnicalContext = None
              ConstitutionCheck = None
              RawContent = ""
              Diagnostics = [ Domain.diagnostic Error ex.Message (Some { Path = path; Line = None }) ] }

  let parseUserStories specPath =
    if not (File.Exists specPath) then
        [], [ Domain.diagnostic Info "Specification artifact is missing." None ]
    else
        try
            let lines = File.ReadAllLines specPath

            let storyRegex =
                Regex(@"^### User Story\s+(\d+)\s*-\s*(.*?)\s*\(Priority:\s*([^)]+)\)")

            let mutable stories = []
            let storyHeadings =
                lines
                |> Array.mapi (fun index line -> index, storyRegex.Match line)
                |> Array.choose (fun (index, m) -> if m.Success then Some(index, m) else None)

            let acceptanceScenarios (block: string array) =
                block
                |> Array.choose (fun line ->
                    let trimmed = line.Trim()

                    if Regex.IsMatch(trimmed, @"^\d+\.\s+\*\*") then
                        Some trimmed
                    else
                        None)
                |> Array.toList

            let description (block: string array) =
                block
                |> Array.map (_.Trim())
                |> Array.tryFind (fun line -> line.Length > 0 && not (line.StartsWith "**") && not (line.StartsWith "###") && not (line.StartsWith "---"))
                |> Option.defaultValue ""

            for position in 0 .. storyHeadings.Length - 1 do
                let index, m = storyHeadings[position]
                let nextIndex =
                    if position + 1 < storyHeadings.Length then
                        fst storyHeadings[position + 1]
                    else
                        lines.Length

                let block = lines[(index + 1) .. (nextIndex - 1)]

                stories <-
                    { Id = "US" + m.Groups[1].Value
                      Title = m.Groups[2].Value.Trim()
                      Priority = Some(m.Groups[3].Value.Trim())
                      Description = description block
                      AcceptanceScenarios = acceptanceScenarios block
                      SourceLocation = Some { Path = specPath; Line = Some(index + 1) } }
                    :: stories

            List.rev stories, []
        with ex ->
            [], [ Domain.diagnostic Error ("Specification artifact is unreadable: " + ex.Message) (Some { Path = specPath; Line = None }) ]

  let parseTasks tasksPath =
    if not (File.Exists tasksPath) then
        [], [ Domain.diagnostic Info "Tasks artifact is missing." None ]
    else
        try
            let lines = File.ReadAllLines tasksPath
            let taskRegex = Regex(@"^- \[(.*?)\]\s+(T\d+)\s+(.*)$")
            let depRegex = Regex(@"T\d+")
            let mutable tasks = []

            for index in 0 .. lines.Length - 1 do
                let m = taskRegex.Match(lines[index])
                if m.Success then
                    let raw = "[" + m.Groups[1].Value + "]"
                    let id = m.Groups[2].Value
                    let title = m.Groups[3].Value.Trim()

                    let deps =
                        depRegex.Matches(title)
                        |> Seq.cast<Match>
                        |> Seq.map (_.Value)
                        |> Seq.filter ((<>) id)
                        |> Seq.distinct
                        |> Seq.toList

                    let story =
                        Regex.Match(title, @"\[(US\d+)\]")
                        |> fun storyMatch -> if storyMatch.Success then Some storyMatch.Groups[1].Value else None

                    let description =
                        if index + 1 < lines.Length && lines[index + 1].StartsWith("  ") then
                            Some(lines[index + 1].Trim())
                        else
                            None

                    tasks <-
                        { Id = id
                          Title = title
                          Description = description
                          RawStatus = raw
                          Dependencies = deps
                          RelatedStoryId = story
                          SourceLocation = Some { Path = tasksPath; Line = Some(index + 1) }
                          Metadata = Map.empty }
                        :: tasks

            List.rev tasks, []
        with ex ->
            [], [ Domain.diagnostic Error ("Tasks artifact is unreadable: " + ex.Message) (Some { Path = tasksPath; Line = None }) ]

  let loadSnapshot repositoryRoot =
    let root = resolveRepositoryRoot repositoryRoot
    let now = DateTimeOffset.UtcNow
    let features = discoverFeatureDirectories root
    let featuresWithStatus =
        features
        |> List.map (fun feature ->
            match feature.ArtifactRoot with
            | Some artifactRoot -> { feature with Status = Some(summarizeFeatureStatus artifactRoot) }
            | None -> feature)
    let selected = featuresWithStatus |> List.tryLast
    let selectedRoot = selected |> Option.bind _.ArtifactRoot

    let stories, storyDiagnostics =
        selectedRoot
        |> Option.map (fun featureRoot -> parseUserStories (Path.Combine(featureRoot, "spec.md")))
        |> Option.defaultValue ([], [])

    let plan =
        selectedRoot |> Option.map loadPlan

    let taskGraph =
        selectedRoot
        |> Option.map (fun featureRoot ->
            let tasks, diagnostics = parseTasks (Path.Combine(featureRoot, "tasks.md"))
            TaskGraphBuilder.build (stories |> List.tryHead |> Option.map _.Id) tasks diagnostics)

    let statusDiagnostics =
        selected
        |> Option.bind _.Status
        |> Option.map _.Diagnostics
        |> Option.defaultValue []

    let planDiagnostics =
        plan |> Option.map _.Diagnostics |> Option.defaultValue []

    let taskGraphDiagnostics =
        taskGraph |> Option.map _.Diagnostics |> Option.defaultValue []

    let diagnostics =
        if List.isEmpty features then
            [ Domain.diagnostic Info "No Speckit feature artifacts were found. Use refresh after creating specs." None ]
        else
            statusDiagnostics @ storyDiagnostics @ planDiagnostics @ taskGraphDiagnostics

    { RepositoryRoot = root
      CurrentBranch = None
      Features = featuresWithStatus
      SelectedFeatureId = selected |> Option.map _.Id
      Stories = stories
      SelectedStoryId = stories |> List.tryHead |> Option.map _.Id
      Plan = plan
      TaskGraph = taskGraph
      SelectedTaskId = taskGraph |> Option.bind (_.SelectedTaskId)
      Panes = Domain.defaultPanes
      Ui = Domain.defaultUiPreferences
      Diagnostics = diagnostics
      LastRefreshedAt = now }
