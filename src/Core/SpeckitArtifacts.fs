namespace SkDashboard.Core

open System
open System.IO
open System.Text.Json
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

  let private themeFeedback severity family (themeId: string option) (source: string option) message failureKind =
    { Severity = severity
      Family = family
      ThemeId = themeId
      Source = source
      Message = message
      FailureKind = failureKind }

  let private familyFolder (configPath: string) family =
    let configDirectory =
        Path.GetDirectoryName configPath
        |> Option.ofObj
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
        |> Option.defaultValue configPath

    Path.Combine(configDirectory, "themes", Domain.themeFamilyId family)

  let private readString (element: JsonElement) (name: string) =
    match element.TryGetProperty(name) with
    | true, value when value.ValueKind = JsonValueKind.String -> value.GetString() |> Option.ofObj
    | _ -> None

  let private readBool (element: JsonElement) (name: string) =
    match element.TryGetProperty(name) with
    | true, value when value.ValueKind = JsonValueKind.True -> Some true
    | true, value when value.ValueKind = JsonValueKind.False -> Some false
    | _ -> None

  let private readInt (element: JsonElement) (name: string) =
    match element.TryGetProperty(name) with
    | true, value when value.ValueKind = JsonValueKind.Number ->
        match value.TryGetInt32() with
        | true, parsed -> Some parsed
        | _ -> None
    | _ -> None

  let private parseColorStyle (element: JsonElement) =
    match element.ValueKind with
    | JsonValueKind.String ->
        element.GetString()
        |> Option.ofObj
        |> Option.map (fun foreground -> { Foreground = foreground; Background = None })
    | JsonValueKind.Object ->
        match readString element "foreground" with
        | Some foreground -> Some { Foreground = foreground; Background = readString element "background" }
        | None -> None
    | _ -> None

  let private parseColors (path: string) family baseColors (element: JsonElement) =
    if element.ValueKind <> JsonValueKind.Object then
        baseColors, [ themeFeedback Warning family None (Some path) "Theme colors must be an object; defaults were used." "unsupported-value" ]
    else
        let mutable colors = baseColors
        let mutable diagnostics = []

        for property in element.EnumerateObject() do
            match Domain.tryColorRole property.Name, parseColorStyle property.Value with
            | Some role, Some style -> colors <- Map.add role style colors
            | Some role, None ->
                diagnostics <-
                    themeFeedback Warning family (Some(Domain.colorRoleId role)) (Some path) ("Invalid color role ignored: " + property.Name) "unsupported-value"
                    :: diagnostics
            | None, _ ->
                diagnostics <-
                    themeFeedback Info family None (Some path) ("Unknown color role ignored: " + property.Name) "unknown-field"
                    :: diagnostics

        colors, List.rev diagnostics

  let private parseCustomAppTheme (path: string) (root: JsonElement) =
    let family = readString root "family" |> Option.map (_.Trim().ToLowerInvariant())

    match family with
    | Some "app" ->
        match readString root "id", readString root "displayName" with
        | Some id, Some displayName when not (String.IsNullOrWhiteSpace id) && not (String.IsNullOrWhiteSpace displayName) ->
            let fallback = Domain.builtInAppThemes |> List.head
            let colors, colorDiagnostics =
                match root.TryGetProperty("colors") with
                | true, value -> parseColors path AppThemeFamily fallback.Colors value
                | _ -> fallback.Colors, []

            let table =
                match root.TryGetProperty("table") with
                | true, value when value.ValueKind = JsonValueKind.Object ->
                    { fallback.Table with
                        Border = readString value "border" |> Option.bind Domain.tryTableBorder |> Option.defaultValue fallback.Table.Border
                        StickyColumns = readInt value "stickyColumns" |> Option.filter ((<=) 0) |> Option.defaultValue fallback.Table.StickyColumns
                        HorizontalStep = readInt value "horizontalStep" |> Option.filter ((<) 0) |> Option.defaultValue fallback.Table.HorizontalStep
                        AlternateRowShading =
                            readBool root "alternateRowShading"
                            |> Option.defaultValue fallback.Table.AlternateRowShading }
                | _ -> fallback.Table

            let mode =
                match readString root "mode" |> Option.map (_.Trim().ToLowerInvariant()) with
                | Some "light" -> Some LightDisplayMode
                | Some "dark" -> Some DarkDisplayMode
                | _ -> None

            Choice1Of2
                { Id = id
                  DisplayName = displayName
                  Source = CustomTheme path
                  Mode = mode
                  Table = table
                  AlternateRowShading = readBool root "alternateRowShading" |> Option.defaultValue false
                  Colors = colors
                  ValidationStatus = if List.isEmpty colorDiagnostics then ThemeValid else ThemeValidWithReplacements
                  Diagnostics = colorDiagnostics }
        | _ ->
            Choice2Of2 [ themeFeedback Warning AppThemeFamily None (Some path) "App theme is missing id or displayName." "incomplete" ]
    | Some other ->
        Choice2Of2 [ themeFeedback Info AppThemeFamily None (Some path) ("Wrong-family theme ignored: " + other) "wrong-family" ]
    | None ->
        Choice2Of2 [ themeFeedback Warning AppThemeFamily None (Some path) "Theme file is missing family." "incomplete" ]

  let private defaultMarkdownColors = (Domain.builtInMarkdownThemes |> List.find (fun theme -> theme.Id = "default")).Colors
  let private defaultMarkdownSpacing = (Domain.builtInMarkdownThemes |> List.find (fun theme -> theme.Id = "default")).Spacing

  let private validThemeColor (value: string) =
    let text = value.Trim().ToLowerInvariant()
    let named =
        Set.ofList
            [ "black"
              "white"
              "grey"
              "gray"
              "grey7"
              "gray7"
              "grey23"
              "gray23"
              "grey42"
              "gray42"
              "green"
              "yellow"
              "red"
              "blue"
              "cyan"
              "magenta"
              "purple"
              "deepskyblue1" ]

    if Set.contains text named then
        true
    elif text.Length = 7 && text[0] = '#' then
        text.Substring(1) |> Seq.forall Uri.IsHexDigit
    else
        false

  let private parseMarkdownColors (path: string) (element: JsonElement) =
    if element.ValueKind <> JsonValueKind.Object then
        defaultMarkdownColors, [ themeFeedback Warning MarkdownThemeFamily None (Some path) "Markdown colors must be an object; defaults were used." "unsupported-value" ]
    else
        let mutable diagnostics = []

        let value name fallback =
            match readString element name with
            | Some candidate when validThemeColor candidate -> candidate
            | Some candidate ->
                diagnostics <-
                    themeFeedback
                        Warning
                        MarkdownThemeFamily
                        None
                        (Some path)
                        (sprintf "Invalid Markdown theme color for %s: %s; using default." name candidate)
                        "unreadable-colors"
                    :: diagnostics

                fallback
            | None -> fallback

        { Normal = value "normal" defaultMarkdownColors.Normal
          Heading = value "heading" defaultMarkdownColors.Heading
          Emphasis = value "emphasis" defaultMarkdownColors.Emphasis
          Strong = value "strong" defaultMarkdownColors.Strong
          Link = value "link" defaultMarkdownColors.Link
          InlineCode = value "inlineCode" defaultMarkdownColors.InlineCode
          CodeBlock = value "codeBlock" defaultMarkdownColors.CodeBlock
          BlockQuote = value "blockQuote" defaultMarkdownColors.BlockQuote
          ListMarker = value "listMarker" defaultMarkdownColors.ListMarker
          CheckedItem = value "checkedItem" defaultMarkdownColors.CheckedItem
          UncheckedItem = value "uncheckedItem" defaultMarkdownColors.UncheckedItem
          Note = value "note" defaultMarkdownColors.Note
          Muted = value "muted" defaultMarkdownColors.Muted },
        List.rev diagnostics

  let private parseMarkdownSpacing (element: JsonElement) =
    if element.ValueKind <> JsonValueKind.Object then
        defaultMarkdownSpacing, []
    else
        let mutable diagnostics = []

        let clamp name value =
            if value < 0 || value > 2 then
                diagnostics <-
                    themeFeedback
                        Warning
                        MarkdownThemeFamily
                        None
                        None
                        (sprintf "Markdown spacing %s must be between 0 and 2; clamped." name)
                        "excessive-spacing"
                    :: diagnostics

            value |> max 0 |> min 2

        { BeforeHeading = readInt element "beforeHeading" |> Option.map (clamp "beforeHeading") |> Option.defaultValue defaultMarkdownSpacing.BeforeHeading
          AfterHeading = readInt element "afterHeading" |> Option.map (clamp "afterHeading") |> Option.defaultValue defaultMarkdownSpacing.AfterHeading
          BetweenParagraphs = readInt element "betweenParagraphs" |> Option.map (clamp "betweenParagraphs") |> Option.defaultValue defaultMarkdownSpacing.BetweenParagraphs
          AroundCodeBlock = readInt element "aroundCodeBlock" |> Option.map (clamp "aroundCodeBlock") |> Option.defaultValue defaultMarkdownSpacing.AroundCodeBlock
          AroundList = readInt element "aroundList" |> Option.map (clamp "aroundList") |> Option.defaultValue defaultMarkdownSpacing.AroundList },
        List.rev diagnostics

  let private parseCustomMarkdownTheme (path: string) (root: JsonElement) =
    let family = readString root "family" |> Option.map (_.Trim().ToLowerInvariant())

    match family with
    | Some "markdown" ->
        match readString root "id", readString root "displayName" with
        | Some id, Some displayName when not (String.IsNullOrWhiteSpace id) && not (String.IsNullOrWhiteSpace displayName) ->
            let colors, colorDiagnostics =
                match root.TryGetProperty("colors") with
                | true, value -> parseMarkdownColors path value
                | _ -> defaultMarkdownColors, []

            let spacing, spacingDiagnostics =
                match root.TryGetProperty("spacing") with
                | true, value -> parseMarkdownSpacing value
                | _ -> defaultMarkdownSpacing, []

            let allDiagnostics = colorDiagnostics @ spacingDiagnostics

            Choice1Of2
                { Id = id
                  DisplayName = displayName
                  Source = CustomTheme path
                  ModeCompatibility = None
                  Colors = colors
                  Spacing = spacing
                  ValidationStatus = if List.isEmpty allDiagnostics then ThemeValid else ThemeValidWithReplacements
                  Diagnostics = allDiagnostics }
        | _ ->
            Choice2Of2 [ themeFeedback Warning MarkdownThemeFamily None (Some path) "Markdown theme is missing id or displayName." "incomplete" ]
    | Some other ->
        Choice2Of2 [ themeFeedback Info MarkdownThemeFamily None (Some path) ("Wrong-family theme ignored: " + other) "wrong-family" ]
    | None ->
        Choice2Of2 [ themeFeedback Warning MarkdownThemeFamily None (Some path) "Theme file is missing family." "incomplete" ]

  let private customThemeFiles (folder: string) =
    if Directory.Exists folder then
        try
            Directory.EnumerateFiles(folder, "*.json")
            |> Seq.sortWith (fun left right -> StringComparer.Ordinal.Compare(left, right))
            |> Seq.toList,
            []
        with ex ->
            [], [ ex.Message ]
    else
        [], []

  let private discoverCustomAppThemes (configPath: string) =
    let folder = familyFolder configPath AppThemeFamily
    let files, folderErrors = customThemeFiles folder
    let mutable diagnostics = folderErrors |> List.map (fun message -> themeFeedback Warning AppThemeFamily None (Some folder) ("App theme folder is unreadable: " + message) "unreadable-folder")
    let mutable themes = []

    for file in files do
        try
            use document = JsonDocument.Parse(File.ReadAllText file)
            match parseCustomAppTheme file document.RootElement with
            | Choice1Of2 theme -> themes <- theme :: themes; diagnostics <- diagnostics @ theme.Diagnostics
            | Choice2Of2 feedback -> diagnostics <- diagnostics @ feedback
        with ex ->
            diagnostics <- diagnostics @ [ themeFeedback Warning AppThemeFamily None (Some file) ("App theme could not be loaded: " + ex.Message) "invalid-json" ]

    List.rev themes, diagnostics

  let private discoverCustomMarkdownThemes (configPath: string) =
    let folder = familyFolder configPath MarkdownThemeFamily
    let files, folderErrors = customThemeFiles folder
    let mutable diagnostics = folderErrors |> List.map (fun message -> themeFeedback Warning MarkdownThemeFamily None (Some folder) ("Markdown theme folder is unreadable: " + message) "unreadable-folder")
    let mutable themes = []

    for file in files do
        try
            use document = JsonDocument.Parse(File.ReadAllText file)
            match parseCustomMarkdownTheme file document.RootElement with
            | Choice1Of2 theme -> themes <- theme :: themes; diagnostics <- diagnostics @ theme.Diagnostics
            | Choice2Of2 feedback -> diagnostics <- diagnostics @ feedback
        with ex ->
            diagnostics <- diagnostics @ [ themeFeedback Warning MarkdownThemeFamily None (Some file) ("Markdown theme could not be loaded: " + ex.Message) "invalid-json" ]

    List.rev themes, diagnostics

  let private dedupeAppThemes (themes: AppTheme list) =
    let mutable seen = Set.empty
    let mutable kept = []
    let mutable diagnostics = []

    for theme in themes do
        if Set.contains theme.Id seen then
            diagnostics <-
                themeFeedback Warning AppThemeFamily (Some theme.Id) None ("Duplicate theme id ignored: " + theme.Id) "duplicate-id"
                :: diagnostics
        else
            seen <- Set.add theme.Id seen
            kept <- theme :: kept

    List.rev kept, List.rev diagnostics

  let private dedupeMarkdownThemes (themes: MarkdownTheme list) =
    let mutable seen = Set.empty
    let mutable kept = []
    let mutable diagnostics = []

    for theme in themes do
        if Set.contains theme.Id seen then
            diagnostics <-
                themeFeedback Warning MarkdownThemeFamily (Some theme.Id) None ("Duplicate theme id ignored: " + theme.Id) "duplicate-id"
                :: diagnostics
        else
            seen <- Set.add theme.Id seen
            kept <- theme :: kept

    List.rev kept, List.rev diagnostics

  let discoverThemeCatalog (configPath: string) =
    let customAppThemes, appDiagnostics = discoverCustomAppThemes configPath
    let customMarkdownThemes, markdownDiagnostics = discoverCustomMarkdownThemes configPath
    let appThemes, appDuplicateDiagnostics = dedupeAppThemes (Domain.builtInAppThemes @ customAppThemes)
    let markdownThemes, markdownDuplicateDiagnostics = dedupeMarkdownThemes (Domain.builtInMarkdownThemes @ customMarkdownThemes)

    { AppThemes = appThemes
      MarkdownThemes = markdownThemes
      Diagnostics = appDiagnostics @ markdownDiagnostics @ appDuplicateDiagnostics @ markdownDuplicateDiagnostics }

  let discoverChecklists (featureRoot: string) =
    let checklistRoot = Path.Combine(featureRoot, "checklists")

    if not (Directory.Exists checklistRoot) then
        [], [ Domain.diagnostic Info "No checklist folder exists for the active feature." (Some { Path = checklistRoot; Line = None }) ]
    else
        try
            let files =
                Directory.EnumerateFiles(checklistRoot, "*.md")
                |> Seq.sortWith (fun left right -> StringComparer.Ordinal.Compare(left, right))
                |> Seq.map (fun (path: string) ->
                    { Id = Path.GetFileNameWithoutExtension path |> Option.ofObj |> Option.defaultValue path
                      DisplayName = Path.GetFileName path |> Option.ofObj |> Option.defaultValue path
                      Path = path }: ChecklistArtifact)
                |> Seq.toList

            if List.isEmpty files then
                [], [ Domain.diagnostic Info "No checklist files exist for the active feature." (Some { Path = checklistRoot; Line = None }) ]
            else
                files, []
        with ex ->
            [], [ Domain.diagnostic Warning ("Checklist folder is unreadable: " + ex.Message) (Some { Path = checklistRoot; Line = None }) ]

  let loadChecklistDocument (checklist: ChecklistArtifact) =
    let source =
        Some
            { Path = checklist.Path
              DisplayName = checklist.DisplayName }

    try
        if not (File.Exists checklist.Path) then
            { Title = checklist.DisplayName
              RawContent = "Checklist is unavailable: " + checklist.Path
              Source = source
              Status = MarkdownSourceMissing
              Diagnostics = [ { Message = "Checklist file is missing."; SourcePath = Some checklist.Path } ] }
        else
            let text = File.ReadAllText checklist.Path

            if String.IsNullOrWhiteSpace text then
                { Title = checklist.DisplayName
                  RawContent = "Checklist file is empty: " + checklist.Path
                  Source = source
                  Status = MarkdownEmptyDocument
                  Diagnostics = [ { Message = "Checklist file is empty."; SourcePath = Some checklist.Path } ] }
            else
                { Title = checklist.DisplayName
                  RawContent = text
                  Source = source
                  Status = MarkdownRendered
                  Diagnostics = [] }
    with ex ->
        { Title = checklist.DisplayName
          RawContent = "Checklist is unreadable: " + checklist.Path + "\n" + ex.Message
          Source = source
          Status = MarkdownSourceUnreadable ex.Message
          Diagnostics = [ { Message = "Checklist file is unreadable: " + ex.Message; SourcePath = Some checklist.Path } ] }

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
      Version = Domain.resolveDashboardVersion ()
      Features = featuresWithStatus
      SelectedFeatureId = selected |> Option.map _.Id
      Stories = stories
      SelectedStoryId = stories |> List.tryHead |> Option.map _.Id
      Plan = plan
      TaskGraph = taskGraph
      SelectedTaskId = taskGraph |> Option.bind (_.SelectedTaskId)
      Panes = Domain.defaultPanes
      Ui = Domain.defaultUiPreferences
      FullScreen = None
      Diagnostics = diagnostics
      LastRefreshedAt = now }
