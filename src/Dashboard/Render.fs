namespace SkDashboard.Dashboard

open System
open System.IO
open System.Text
open NTokenizers.Extensions.Spectre.Console
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

    let paddedMarkup role ui text =
        sprintf "[%s] %s [/]" (styleTag role ui) (Markup.Escape text)

    let color role ui = (styleFor role ui).Foreground

    let rowStripeRole index =
        if index % 2 = 0 then RowStripeEven else RowStripeOdd

    let rowStripeTag index (ui: DashboardUiPreferences) = styleTag (rowStripeRole index) ui

    let stripedMarkup index (ui: DashboardUiPreferences) text =
        let tag =
            if ui.Table.AlternateRowShading then
                rowStripeTag index ui
            else
                styleTag DetailBody ui

        sprintf "[%s] %s [/]" tag (Markup.Escape text)

    let styledCellMarkup rowRole ui text =
        sprintf "[%s] %s [/]" (styleTag rowRole ui) (Markup.Escape text)

    let tableBorder border =
        match border with
        | NoBorder -> TableBorder.None
        | MinimalBorder -> TableBorder.Minimal
        | RoundedBorder -> TableBorder.Rounded
        | HeavyBorder -> TableBorder.Heavy

    let dashboardTable (ui: DashboardUiPreferences) =
        Table().Border(tableBorder ui.Table.Border).Expand()

    let paneScroll kind (snapshot: DashboardSnapshot) =
        snapshot.Panes
        |> List.tryFind (fun pane -> pane.Kind = kind)
        |> Option.map _.ScrollOffset
        |> Option.defaultValue 0

    let sliceText offset text =
        if offset <= 0 || String.IsNullOrEmpty text then text
        elif offset >= text.Length then ""
        else text.Substring offset

    let visibleRows selectedIndex visibleCount items =
        let itemCount = List.length items
        let visibleCount = max 1 (min 3 visibleCount)
        let centeredOffset = selectedIndex - (visibleCount / 2)
        let rowOffset = centeredOffset |> max 0 |> min (max 0 (itemCount - visibleCount))

        let viewport =
            { Domain.defaultTableViewport visibleCount 1 with
                RowOffset = rowOffset }
            |> Domain.clampTableViewport itemCount 1

        items |> List.skip viewport.RowOffset |> List.truncate viewport.VisibleRows, viewport

    let tableVisibleRowCount () = 3

    let scrollIndicator viewport totalRows totalColumns =
        let vertical =
            if totalRows <= viewport.VisibleRows then
                ""
            else
                sprintf
                    " rows %d-%d/%d"
                    (viewport.RowOffset + 1)
                    (min totalRows (viewport.RowOffset + viewport.VisibleRows))
                    totalRows

        let horizontal =
            if viewport.ColumnOffset <= 0 && totalColumns <= viewport.VisibleColumns then
                ""
            else
                sprintf " col +%d" viewport.ColumnOffset

        vertical + horizontal

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
        let storyTasks =
            tasks |> List.filter (fun task -> task.RelatedStoryId = Some storyId)

        let total = List.length storyTasks
        let doneCount = storyTasks |> List.filter taskDone |> List.length
        doneCount, total

    let sourceLine (task: SpeckitTask) =
        task.SourceLocation |> Option.bind _.Line |> Option.defaultValue 0

    let activityTask tasks =
        let storyTasks = tasks |> List.filter (fun task -> task.RelatedStoryId.IsSome)

        let active =
            storyTasks
            |> List.filter (fun task -> task.RawStatus.Trim() <> "[ ]")
            |> List.sortBy sourceLine
            |> List.tryLast

        active |> Option.orElse (storyTasks |> List.sortBy sourceLine |> List.tryLast)

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
                if doneCount = total then
                    color ProgressComplete ui
                else
                    completeColor

            sprintf
                "[%s]%s[/][%s]%s[/] [bold]%d/%d[/]"
                completeColor
                (String('█', filled))
                (color ProgressIncomplete ui)
                (String('░', empty))
                doneCount
                total

    let panel title (renderable: IRenderable) =
        Panel(renderable).Header(PanelHeader(title)).Border(BoxBorder.Rounded)

    let fallbackMarkdownLines text =
        if String.IsNullOrWhiteSpace text then
            [| "(empty document)" |]
        else
            text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')

    let renderMarkdownLines (sourcePath: string option) text =
        try
            use writer = new StringWriter()

            let output = AnsiConsoleOutput(writer)
            let settings = AnsiConsoleSettings()
            settings.Ansi <- AnsiSupport.No
            settings.ColorSystem <- ColorSystemSupport.NoColors
            settings.Out <- output
            settings.Interactive <- InteractionSupport.No

            let console = AnsiConsole.Create(settings)
            AnsiConsoleMarkdownExtensions.WriteMarkdown(console, text)

            let rendered = writer.ToString().Replace("\r\n", "\n").Replace('\r', '\n')

            let lines =
                if String.IsNullOrWhiteSpace rendered then
                    fallbackMarkdownLines text
                else
                    rendered.Split('\n', StringSplitOptions.None)

            lines, MarkdownRendered, []
        with ex ->
            fallbackMarkdownLines text,
            MarkdownPlainTextFallback ex.Message,
            [ { Message = "Markdown renderer failed: " + ex.Message
                SourcePath = sourcePath } ]

    let markdownRows (ui: DashboardUiPreferences) text sourcePath viewport =
        let _, status, diagnostics = renderMarkdownLines sourcePath text

        let theme = ui.Markdown

        let sourceLines =
            let mutable inCode = false

            fallbackMarkdownLines text
            |> Array.collect (fun line ->
                let trimmed = line.TrimStart()
                let isFence = line.Trim().StartsWith("```")
                let isHeading = not inCode && trimmed.StartsWith("#")

                let expanded =
                    if isHeading then
                        Array.concat
                            [ Array.create (max 0 theme.Spacing.BeforeHeading) ""
                              [| line |]
                              Array.create (max 0 theme.Spacing.AfterHeading) "" ]
                    else
                        [| line |]

                if isFence then
                    inCode <- not inCode

                expanded)

        let widest = sourceLines |> Array.map _.Length |> Array.append [| 0 |] |> Array.max
        let viewport = Domain.clampDetailViewport sourceLines.Length widest viewport

        let colorTag value = value

        let wrap tag value =
            "[" + tag + "]" + value + "[/]"

        let inlineMarkdown normalColor (value: string) =
            let output = StringBuilder()
            let literal = StringBuilder()

            let flushLiteral () =
                if literal.Length > 0 then
                    output.Append(wrap (colorTag normalColor) (Markup.Escape(literal.ToString()))) |> ignore
                    literal.Clear() |> ignore

            let appendStyled (tag: string) (text: string) =
                flushLiteral ()

                output.Append("[").Append(tag).Append("]").Append(Markup.Escape text).Append("[/]")
                |> ignore

            let rec loop index =
                if index >= value.Length then
                    ()
                elif index + 1 < value.Length && value[index] = '*' && value[index + 1] = '*' then
                    let close = value.IndexOf("**", index + 2, StringComparison.Ordinal)

                    if close > index then
                        appendStyled ("bold " + theme.Colors.Strong) value[(index + 2) .. (close - 1)]
                        loop (close + 2)
                    else
                        literal.Append(value[index]) |> ignore
                        loop (index + 1)
                elif value[index] = '`' then
                    let close = value.IndexOf('`', index + 1)

                    if close > index then
                        appendStyled theme.Colors.InlineCode value[(index + 1) .. (close - 1)]
                        loop (close + 1)
                    else
                        literal.Append(value[index]) |> ignore
                        loop (index + 1)
                elif value[index] = '_' then
                    let close = value.IndexOf('_', index + 1)

                    if close > index then
                        appendStyled ("italic " + theme.Colors.Emphasis) value[(index + 1) .. (close - 1)]
                        loop (close + 1)
                    else
                        literal.Append(value[index]) |> ignore
                        loop (index + 1)
                elif value[index] = '[' then
                    let labelClose = value.IndexOf(']', index + 1)

                    if
                        labelClose > index
                        && labelClose + 1 < value.Length
                        && value[labelClose + 1] = '('
                    then
                        let urlClose = value.IndexOf(')', labelClose + 2)

                        if urlClose > labelClose then
                            flushLiteral ()

                            output
                                .Append("[underline ")
                                .Append(theme.Colors.Link)
                                .Append("]")
                                .Append(Markup.Escape value[(index + 1) .. (labelClose - 1)])
                                .Append("[/] [")
                                .Append(theme.Colors.Muted)
                                .Append("](")
                                .Append(Markup.Escape value[(labelClose + 2) .. (urlClose - 1)])
                                .Append(")[/]")
                            |> ignore

                            loop (urlClose + 1)
                        else
                            literal.Append(value[index]) |> ignore
                            loop (index + 1)
                    else
                        literal.Append(value[index]) |> ignore
                        loop (index + 1)
                else
                    literal.Append(value[index]) |> ignore
                    loop (index + 1)

            loop 0
            flushLiteral ()
            output.ToString()

        let renderLine (inCode: bool) (line: string) =
            let trimmed = line.TrimStart()

            if String.IsNullOrWhiteSpace line then
                Text(" ") :> IRenderable
            elif line.Trim().StartsWith("```") then
                Markup(wrap theme.Colors.CodeBlock (Markup.Escape line)) :> IRenderable
            elif inCode then
                Markup(wrap theme.Colors.CodeBlock (Markup.Escape line)) :> IRenderable
            elif trimmed.StartsWith(">") then
                Markup(wrap theme.Colors.BlockQuote (Markup.Escape line)) :> IRenderable
            elif trimmed.StartsWith("#") then
                let heading = trimmed.TrimStart('#').Trim()
                Markup("[bold " + theme.Colors.Heading + "]" + inlineMarkdown theme.Colors.Heading heading + "[/]") :> IRenderable
            elif trimmed.StartsWith("- ") || trimmed.StartsWith("* ") then
                let indent = line.Length - trimmed.Length
                let body = trimmed.Substring(2)
                let markerColor, bodyText =
                    if body.StartsWith("[x]", StringComparison.OrdinalIgnoreCase) then
                        theme.Colors.CheckedItem, body
                    elif body.StartsWith("[ ]", StringComparison.Ordinal) then
                        theme.Colors.UncheckedItem, body
                    else
                        theme.Colors.ListMarker, body

                Markup(
                    String(' ', indent)
                    + wrap markerColor "-"
                    + " "
                    + inlineMarkdown markerColor bodyText
                )
                :> IRenderable
            else
                let dot = trimmed.IndexOf(". ", StringComparison.Ordinal)

                if dot > 0 && trimmed[0 .. (dot - 1)] |> Seq.forall Char.IsDigit then
                    let indent = line.Length - trimmed.Length
                    let marker = trimmed[0..dot]
                    let body = trimmed.Substring(dot + 2)
                    Markup(
                        String(' ', indent)
                        + wrap theme.Colors.ListMarker marker
                        + " "
                        + inlineMarkdown theme.Colors.Normal body
                    )
                    :> IRenderable
                else
                    Markup(inlineMarkdown theme.Colors.Normal line) :> IRenderable

        let formatted =
            let mutable inCode = false

            sourceLines
            |> Array.map (fun line ->
                let renderable = renderLine inCode line

                if line.Trim().StartsWith("```") then
                    inCode <- not inCode

                renderable)

        let rows =
            formatted
            |> Array.skip viewport.LineOffset
            |> Array.truncate viewport.VisibleLines
            |> Array.mapi (fun index renderable ->
                if viewport.ColumnOffset = 0 then
                    renderable
                else
                    let sourceIndex = viewport.LineOffset + index
                    Text(sliceText viewport.ColumnOffset sourceLines.[sourceIndex]) :> IRenderable)

        rows, sourceLines.Length, viewport, status, diagnostics

    let markdownBlock ui text sourcePath =
        let lines, _, _, status, diagnostics =
            markdownRows ui text sourcePath (Domain.defaultDetailViewport System.Int32.MaxValue 120)

        let _ = status, diagnostics
        Rows lines :> IRenderable


    let featuresTable snapshot =
        let ui = snapshot.Ui
        let columnOffset = paneScroll Features snapshot
        let table = dashboardTable ui

        table.AddColumn(TableColumn(sprintf "[bold %s]Feature[/]" (color Muted ui)))
        |> ignore

        table.AddColumn(TableColumn(sprintf "[bold %s]Checkout[/]" (color Muted ui)))
        |> ignore

        table.AddColumn(TableColumn(sprintf "[bold %s]Artifacts[/]" (color Muted ui)))
        |> ignore

        match snapshot.Features with
        | [] ->
            table.AddRow(
                Markup("[yellow]No feature artifacts found[/]"),
                Markup(""),
                Markup("[grey]Create specs with Speckit, then press r.[/]")
            )
            |> ignore
        | features ->
            let selectedIndex =
                features
                |> List.tryFindIndex (fun feature -> Some feature.Id = snapshot.SelectedFeatureId)
                |> Option.defaultValue 0

            let visible, viewport =
                visibleRows selectedIndex (tableVisibleRowCount ()) features
                |> fun (rows, view) ->
                    rows,
                    { view with
                        ColumnOffset = columnOffset }

            if not (String.IsNullOrWhiteSpace(scrollIndicator viewport features.Length 120)) then
                table.Caption(
                    TableTitle("[grey]" + Markup.Escape(scrollIndicator viewport features.Length 120) + "[/]")
                )
                |> ignore

            for absoluteIndex, feature in visible |> List.mapi (fun i value -> viewport.RowOffset + i, value) do
                let name =
                    if feature.IsSelected then
                        styledCellMarkup Selected ui (sliceText columnOffset feature.DisplayName)
                    else
                        stripedMarkup absoluteIndex ui (sliceText columnOffset feature.DisplayName)

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

                let checkout =
                    if feature.IsSelected then
                        styledCellMarkup Selected ui (checkoutText feature.CheckoutState)
                    else
                        stripedMarkup absoluteIndex ui (checkoutText feature.CheckoutState)

                let artifactsText =
                    match feature.Status with
                    | None -> "no artifacts"
                    | Some status ->
                        sprintf
                            "S:%s P:%s T:%s C:%s"
                            (artifactText status.SpecState)
                            (artifactText status.PlanState)
                            (artifactText status.TasksState)
                            (artifactText status.ChecklistState)

                let artifactsCell =
                    if feature.IsSelected then
                        artifacts
                    else
                        stripedMarkup absoluteIndex ui artifactsText

                table.AddRow(Markup(name), Markup(checkout), Markup(artifactsCell)) |> ignore

        table

    let storiesTable snapshot =
        let ui = snapshot.Ui
        let columnOffset = paneScroll Stories snapshot
        let tasks = featureTasks snapshot
        let lastActivityStoryId = activityStoryId tasks
        let table = dashboardTable ui

        table.AddColumn(TableColumn(sprintf "[bold %s]Story[/]" (color Muted ui)))
        |> ignore

        table.AddColumn(TableColumn(sprintf "[bold %s]Progress[/]" (color Muted ui)))
        |> ignore

        match snapshot.Stories with
        | [] -> table.AddRow(Markup("[yellow]No user stories[/]"), Markup("")) |> ignore
        | stories ->
            let selectedIndex =
                stories
                |> List.tryFindIndex (fun story -> Some story.Id = snapshot.SelectedStoryId)
                |> Option.defaultValue 0

            let visible, viewport =
                visibleRows selectedIndex (tableVisibleRowCount ()) stories
                |> fun (rows, view) ->
                    rows,
                    { view with
                        ColumnOffset = columnOffset }

            if not (String.IsNullOrWhiteSpace(scrollIndicator viewport stories.Length 120)) then
                table.Caption(TableTitle("[grey]" + Markup.Escape(scrollIndicator viewport stories.Length 120) + "[/]"))
                |> ignore

            for absoluteIndex, story in visible |> List.mapi (fun i value -> viewport.RowOffset + i, value) do
                let doneCount, total = storyProgress tasks story.Id
                let storyLabel = sliceText columnOffset (story.Id + " " + story.Title)

                let storyText =
                    if Some story.Id = snapshot.SelectedStoryId then
                        sprintf "[%s] %s [/]" (styleTag Selected ui) (Markup.Escape storyLabel)
                    elif Some story.Id = lastActivityStoryId then
                        sprintf "[%s] %s [/]" (styleTag LastActivity ui) (Markup.Escape storyLabel)
                    else
                        stripedMarkup absoluteIndex ui storyLabel

                let progress =
                    if Some story.Id = snapshot.SelectedStoryId then
                        styledCellMarkup Selected ui (sprintf "%d/%d" doneCount total)
                    elif Some story.Id = lastActivityStoryId then
                        styledCellMarkup LastActivity ui (sprintf "%d/%d" doneCount total)
                    else
                        stripedMarkup absoluteIndex ui (sprintf "%d/%d" doneCount total)

                table.AddRow(Markup(storyText), Markup(progress)) |> ignore

        table

    let planRenderable snapshot =
        match snapshot.Plan with
        | None -> Markup("[grey]No plan loaded.[/]") :> IRenderable
        | Some plan ->
            let text =
                plan.Summary
                |> Option.orElse (
                    if String.IsNullOrWhiteSpace plan.RawContent then
                        None
                    else
                        Some plan.RawContent
                )
                |> Option.defaultValue "Plan artifact is empty or missing."

            Markup("[white]" + Markup.Escape text + "[/]") :> IRenderable

    let tasksTable snapshot =
        let ui = snapshot.Ui
        let columnOffset = paneScroll TaskGraph snapshot
        let lastActivityTaskId = featureTasks snapshot |> activityTask |> Option.map _.Id
        let table = dashboardTable ui

        table.AddColumn(TableColumn(sprintf "[bold %s]Task[/]" (color Muted ui)))
        |> ignore

        table.AddColumn(TableColumn(sprintf "[bold %s]Status[/]" (color Muted ui)))
        |> ignore

        table.AddColumn(TableColumn(sprintf "[bold %s]Title[/]" (color Muted ui)))
        |> ignore

        match snapshot.TaskGraph with
        | None ->
            table.AddRow(Markup("[yellow]No task graph[/]"), Markup(""), Markup(""))
            |> ignore
        | Some graph when List.isEmpty graph.Nodes ->
            table.AddRow(Markup("[yellow]No tasks for selected story[/]"), Markup(""), Markup(""))
            |> ignore
        | Some graph ->
            let selectedIndex =
                graph.Nodes
                |> List.tryFindIndex (fun task -> Some task.Id = graph.SelectedTaskId)
                |> Option.defaultValue 0

            let visible, viewport =
                visibleRows selectedIndex (tableVisibleRowCount ()) graph.Nodes
                |> fun (rows, view) ->
                    rows,
                    { view with
                        ColumnOffset = columnOffset }

            if not (String.IsNullOrWhiteSpace(scrollIndicator viewport graph.Nodes.Length 120)) then
                table.Caption(
                    TableTitle(
                        "[grey]"
                        + Markup.Escape(scrollIndicator viewport graph.Nodes.Length 120)
                        + "[/]"
                    )
                )
                |> ignore

            visible
            |> List.iteri (fun relativeIndex task ->
                let index = viewport.RowOffset + relativeIndex
                let selected = Some task.Id = graph.SelectedTaskId
                let lastActivity = Some task.Id = lastActivityTaskId

                let id =
                    if selected then
                        styledCellMarkup Selected ui task.Id
                    elif lastActivity then
                        styledCellMarkup LastActivity ui task.Id
                    else
                        stripedMarkup index ui task.Id

                let status =
                    if lastActivity then
                        styledCellMarkup LastActivity ui task.RawStatus
                    elif selected then
                        styledCellMarkup Selected ui task.RawStatus
                    elif taskDone task then
                        stripedMarkup index ui task.RawStatus
                    else
                        stripedMarkup index ui task.RawStatus

                let title =
                    if selected then
                        styledCellMarkup Selected ui (sliceText columnOffset task.Title)
                    elif lastActivity then
                        styledCellMarkup LastActivity ui (sliceText columnOffset task.Title)
                    else
                        stripedMarkup index ui (sliceText columnOffset task.Title)

                table.AddRow(Markup(id), Markup(status), Markup(title)) |> ignore)

        table

    let detailRenderable snapshot =
        let ui = snapshot.Ui

        match snapshot.TaskGraph, snapshot.SelectedTaskId with
        | Some graph, Some selectedTaskId ->
            graph.Nodes
            |> List.tryFind (fun task -> task.Id = selectedTaskId)
            |> Option.map (fun task ->
                let deps =
                    if List.isEmpty task.Dependencies then
                        "(none)"
                    else
                        String.concat ", " task.Dependencies

                let source =
                    task.SourceLocation
                    |> Option.map (fun source ->
                        source.Path
                        + ":"
                        + (source.Line |> Option.map string |> Option.defaultValue "?"))
                    |> Option.defaultValue "(unknown)"

                let description = task.Description |> Option.defaultValue ""

                let rows =
                    Rows(
                        [| Markup(sprintf "[bold %s]%s[/]" (color DetailHeading ui) (Markup.Escape task.Title))
                           :> IRenderable
                           Rule(sprintf "[%s]details[/]" (color Muted ui)) :> IRenderable
                           Markup(
                               sprintf
                                   "[%s]status[/] [%s]%s[/]"
                                   (color DetailLabel ui)
                                   (color DetailBody ui)
                                   (Markup.Escape task.RawStatus)
                           )
                           :> IRenderable
                           Markup(
                               sprintf
                                   "[%s]story[/] [%s]%s[/]"
                                   (color DetailLabel ui)
                                   (color DetailBody ui)
                                   (task.RelatedStoryId |> Option.defaultValue "(none)" |> Markup.Escape)
                           )
                           :> IRenderable
                           Markup(
                               sprintf
                                   "[%s]deps[/] [%s]%s[/]"
                                   (color DetailLabel ui)
                                   (color DetailBody ui)
                                   (Markup.Escape deps)
                           )
                           :> IRenderable
                           Markup(
                               sprintf
                                   "[%s]source[/] [%s]%s[/]"
                                   (color DetailLabel ui)
                                   (color DetailSource ui)
                                   (Markup.Escape source)
                           )
                           :> IRenderable
                           markdownBlock ui description (task.SourceLocation |> Option.map _.Path) |]
                    )

                rows :> IRenderable)
            |> Option.defaultValue (Markup("[grey]No selected task.[/]") :> IRenderable)
        | _ -> Markup("[grey]No selected task.[/]") :> IRenderable

    let diagnosticsRenderable snapshot =
        let ui = snapshot.Ui
        let table = dashboardTable ui

        table.AddColumn(TableColumn(sprintf "[bold %s]Severity[/]" (color Muted ui)))
        |> ignore

        table.AddColumn(TableColumn(sprintf "[bold %s]Message[/]" (color Muted ui)))
        |> ignore

        match snapshot.Diagnostics with
        | [] ->
            table.AddRow(Markup("[green]ok[/]"), Markup("[grey]No diagnostics[/]"))
            |> ignore
        | diagnostics ->
            for index, diagnostic in diagnostics |> List.indexed do
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

                match diagnostic.Severity with
                | Info ->
                    table.AddRow(
                        Markup(stripedMarkup index ui (severityText diagnostic.Severity)),
                        Markup(stripedMarkup index ui message)
                    )
                    |> ignore
                | Warning
                | Error ->
                    table.AddRow(
                        Markup(styledCellMarkup role ui (severityText diagnostic.Severity)),
                        Markup(styledCellMarkup role ui message)
                    )
                    |> ignore

        table

    let readSourceText path =
        try
            if File.Exists path then
                File.ReadAllText path
            else
                "Source artifact is missing: " + path
        with ex ->
            "Source artifact is unreadable: " + ex.Message

    let selectedFeature (snapshot: DashboardSnapshot) (modal: FullScreenModal) =
        modal.SelectedFeatureId
        |> Option.orElse snapshot.SelectedFeatureId
        |> Option.bind (fun id -> snapshot.Features |> List.tryFind (fun feature -> feature.Id = id))

    let selectedStory (snapshot: DashboardSnapshot) (modal: FullScreenModal) =
        modal.SelectedStoryId
        |> Option.orElse snapshot.SelectedStoryId
        |> Option.bind (fun id -> snapshot.Stories |> List.tryFind (fun story -> story.Id = id))
        |> Option.orElse (
            featureTasks snapshot
            |> activityTask
            |> Option.bind _.RelatedStoryId
            |> Option.bind (fun id -> snapshot.Stories |> List.tryFind (fun story -> story.Id = id))
        )

    let selectedTask (snapshot: DashboardSnapshot) (modal: FullScreenModal) =
        match snapshot.TaskGraph with
        | None -> None
        | Some graph ->
            modal.SelectedTaskId
            |> Option.orElse graph.SelectedTaskId
            |> Option.bind (fun id -> graph.Nodes |> List.tryFind (fun task -> task.Id = id))
            |> Option.orElse (featureTasks snapshot |> activityTask)

    let sourceLocationText (source: SourceLocation option) =
        source
        |> Option.map (fun location ->
            location.Path
            + (location.Line |> Option.map (sprintf ":%d") |> Option.defaultValue ""))
        |> Option.defaultValue "(unknown)"

    let fullScreenText (snapshot: DashboardSnapshot) (modal: FullScreenModal) =
        match modal.Target with
        | FeatureFullScreen ->
            match selectedFeature snapshot modal with
            | None -> "No selected feature is available. Press esc to return."
            | Some feature ->
                let status =
                    feature.Status
                    |> Option.map (fun value ->
                        sprintf
                            "spec=%s\nplan=%s\ntasks=%s\nchecklists=%s"
                            (artifactText value.SpecState)
                            (artifactText value.PlanState)
                            (artifactText value.TasksState)
                            (artifactText value.ChecklistState))
                    |> Option.defaultValue "no artifact status"

                let source =
                    feature.ArtifactRoot
                    |> Option.map (fun root ->
                        [ "spec.md"; "plan.md"; "tasks.md" ]
                        |> List.map (fun name -> "## " + name + "\n" + readSourceText (Path.Combine(root, name)))
                        |> String.concat "\n\n")
                    |> Option.defaultValue "No feature artifact root is available."

                sprintf
                    "Feature %s\nName: %s\nBranch: %s\nCheckout: %s\nArtifact root: %s\n\n%s\n\n%s"
                    feature.Id
                    feature.DisplayName
                    (feature.BranchName |> Option.defaultValue "(none)")
                    (checkoutText feature.CheckoutState)
                    (feature.ArtifactRoot |> Option.defaultValue "(unknown)")
                    status
                    source
        | StoryFullScreen ->
            match selectedStory snapshot modal with
            | None -> "No selected or active user story is available. Press esc to return."
            | Some story ->
                let tasks =
                    featureTasks snapshot
                    |> List.filter (fun task -> task.RelatedStoryId = Some story.Id)

                let doneCount, total = storyProgress tasks story.Id

                let related =
                    tasks
                    |> List.map (fun task -> task.Id + " " + task.RawStatus + " " + task.Title)
                    |> String.concat "\n"

                let source =
                    story.SourceLocation
                    |> Option.map (fun location -> readSourceText location.Path)
                    |> Option.defaultValue "No source text is available for this story."

                sprintf
                    "User Story %s\nTitle: %s\nPriority: %s\nProgress: %d/%d\nSource: %s\n\nDescription:\n%s\n\nAcceptance Scenarios:\n%s\n\nRelated Tasks:\n%s\n\n%s"
                    story.Id
                    story.Title
                    (story.Priority |> Option.defaultValue "(none)")
                    doneCount
                    total
                    (sourceLocationText story.SourceLocation)
                    story.Description
                    (String.concat "\n" story.AcceptanceScenarios)
                    (if String.IsNullOrWhiteSpace related then
                         "(none)"
                     else
                         related)
                    source
        | PlanFullScreen ->
            match snapshot.Plan with
            | None -> "No plan is loaded for the selected feature. Press esc to return."
            | Some plan ->
                sprintf
                    "Plan\nPath: %s\n\nSummary:\n%s\n\nTechnical Context:\n%s\n\nConstitution Check:\n%s\n\n%s"
                    (plan.Path |> Option.defaultValue "(unknown)")
                    (plan.Summary |> Option.defaultValue "(none)")
                    (plan.TechnicalContext |> Option.defaultValue "(none)")
                    (plan.ConstitutionCheck |> Option.defaultValue "(none)")
                    (if String.IsNullOrWhiteSpace plan.RawContent then
                         "Plan source text is unavailable."
                     else
                         plan.RawContent)
        | TaskFullScreen ->
            match selectedTask snapshot modal with
            | None -> "No selected or active task is available. Press esc to return."
            | Some task ->
                let source =
                    task.SourceLocation
                    |> Option.map (fun location -> readSourceText location.Path)
                    |> Option.defaultValue "No source text is available for this task."

                sprintf
                    "Task %s\nStatus: %s\nTitle: %s\nStory: %s\nDependencies: %s\nSource: %s\nMetadata: %s\n\nDescription:\n%s\n\n%s"
                    task.Id
                    task.RawStatus
                    task.Title
                    (task.RelatedStoryId |> Option.defaultValue "(none)")
                    (if List.isEmpty task.Dependencies then
                         "(none)"
                     else
                         String.concat ", " task.Dependencies)
                    (sourceLocationText task.SourceLocation)
                    (if Map.isEmpty task.Metadata then
                         "(none)"
                     else
                         task.Metadata
                         |> Seq.map (fun kv -> kv.Key + "=" + kv.Value)
                         |> String.concat ", ")
                    (task.Description |> Option.defaultValue "")
                    source
        | ConstitutionFullScreen ->
            modal.Document
            |> Option.map _.RawContent
            |> Option.defaultValue "No constitution document is loaded. Press esc to return."
        | ChecklistFullScreen ->
            modal.Checklist
            |> Option.bind _.Document
            |> Option.orElse modal.Document
            |> Option.map _.RawContent
            |> Option.defaultValue "No checklist document is loaded. Press esc to return."
        | SettingsFullScreen ->
            let ui = snapshot.Ui
            let focus =
                modal.Viewport.LineOffset |> max 0 |> min 3

            let marker index = if focus = index then "> " else "  "
            let fallbackText =
                [ if ui.Themes.AppThemeFallback then "app fallback active"
                  if ui.Themes.MarkdownThemeFallback then "markdown fallback active" ]
                |> function
                    | [] -> "none"
                    | values -> String.concat ", " values

            sprintf
                "Settings\n%sApp theme: %s\n%sMarkdown theme: %s\nResolved Markdown theme: %s\nApp theme choices: %s\nMarkdown theme choices: %s\nTheme fallback: %s\n%sTable border: %s\nSticky columns: %d\nTable horizontal step: %d\nDetail wrap: %b\n%sDetail horizontal step: %d\nLive reload: %b\nLive reload debounce: %dms\n\nUp/down selects a setting. Left/right changes it and saves automatically. A/a and M/m also change themes. 2 reloads, Esc closes."
                (marker 0)
                ui.Themes.SelectedAppThemeId
                (marker 1)
                ui.Themes.SelectedMarkdownThemeId
                ui.Markdown.Id
                (String.concat ", " ui.Themes.AvailableAppThemes)
                (String.concat ", " ui.Themes.AvailableMarkdownThemes)
                fallbackText
                (marker 2)
                (Domain.tableBorderId ui.Table.Border)
                ui.Table.StickyColumns
                ui.Table.HorizontalStep
                ui.Detail.WrapText
                (marker 3)
                ui.Detail.HorizontalStep
                ui.LiveReload.Enabled
                ui.LiveReload.DebounceMilliseconds

    let fullScreenTitle target =
        match target with
        | FeatureFullScreen -> "Feature"
        | StoryFullScreen -> "User Story"
        | PlanFullScreen -> "Plan"
        | TaskFullScreen -> "Task"
        | ConstitutionFullScreen -> "Constitution"
        | ChecklistFullScreen -> "Checklists"
        | SettingsFullScreen -> "Settings"

    let fullScreenRenderable (snapshot: DashboardSnapshot) (modal: FullScreenModal) =
        let ui = snapshot.Ui
        let rawText = fullScreenText snapshot modal

        let sourcePath = modal.Document |> Option.bind _.Source |> Option.map _.Path

        let rows, lineCount, viewport, status, diagnostics =
            match modal.Target with
            | SettingsFullScreen ->
                let lines = rawText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
                let widest = lines |> Array.map _.Length |> Array.append [| 0 |] |> Array.max
                let viewport = Domain.clampDetailViewport lines.Length widest modal.Viewport

                let rows =
                    lines
                    |> Array.skip viewport.LineOffset
                    |> Array.truncate viewport.VisibleLines
                    |> Array.map (fun line ->
                        Markup("[white]" + Markup.Escape(sliceText viewport.ColumnOffset line) + "[/]") :> IRenderable)

                rows, lines.Length, viewport, MarkdownRendered, []
            | _ -> markdownRows ui rawText sourcePath modal.Viewport

        let title =
            let statusText =
                match status, diagnostics with
                | MarkdownPlainTextFallback _, _ -> " fallback"
                | MarkdownEmptyDocument, _ -> " empty"
                | MarkdownSourceMissing, _ -> " missing"
                | MarkdownSourceUnreadable _, _ -> " unreadable"
                | _, _ :: _ -> " diagnostics"
                | _ -> ""

            sprintf
                "[bold %s]%s Full Screen[/] [grey]lines %d-%d/%d col +%d%s[/]"
                (color PanelAccent ui)
                (fullScreenTitle modal.Target)
                (viewport.LineOffset + 1)
                (min lineCount (viewport.LineOffset + viewport.VisibleLines))
                lineCount
                viewport.ColumnOffset
                statusText

        panel title (Rows rows)

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
                    let marker =
                        if Some story.Id = snapshot.SelectedStoryId then
                            "> "
                        else
                            "  "

                    let activity =
                        if Some story.Id = lastActivityStoryId then
                            " activity"
                        else
                            ""

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

                    let activity =
                        if Some task.Id = lastActivityTaskId then
                            " activity"
                        else
                            ""

                    sprintf "%s%s%s %s %s" marker task.Id activity task.RawStatus task.Title)
                |> String.concat "\n"

        let paneOffsets =
            snapshot.Panes
            |> List.map (fun pane -> sprintf "%s:%d" pane.Id pane.ScrollOffset)
            |> String.concat ","

        let fullScreenState =
            snapshot.FullScreen
            |> Option.map (fun modal ->
                sprintf
                    "%s:%d:%d:%d:%d"
                    (fullScreenTitle modal.Target)
                    modal.Viewport.LineOffset
                    modal.Viewport.ColumnOffset
                    modal.Viewport.VisibleLines
                    modal.Viewport.VisibleColumns)
            |> Option.defaultValue "(none)"

        sprintf
            "sk-dashboard %s\nRepository: %s\nCurrent branch: %s\nFeatures:\n%s\nStories:\n%s\nTasks:\n%s\nPanes:%s\nFullScreen:%s\nDiagnostics:%d"
            snapshot.Version.Label
            snapshot.RepositoryRoot
            (snapshot.CurrentBranch |> Option.defaultValue "(none)")
            featureText
            storyText
            taskText
            paneOffsets
            fullScreenState
            snapshot.Diagnostics.Length

    let snapshotRenderableForWidth width snapshot : IRenderable =
        let ui = snapshot.Ui

        let header =
            let branch = snapshot.CurrentBranch |> Option.defaultValue "(none)" |> Markup.Escape

            Panel(
                Markup(
                    sprintf
                        "[bold %s]sk-dashboard[/] [bold white]%s[/]  [%s]repo[/] [white]%s[/]  [%s]branch[/] [yellow]%s[/]"
                        (color PanelAccent ui)
                        (Markup.Escape snapshot.Version.Label)
                        (color Muted ui)
                        (Markup.Escape snapshot.RepositoryRoot)
                        (color Muted ui)
                        branch
                )
            )
                .Border(BoxBorder.None)

        let left = Layout("left").Ratio(3)

        left.SplitRows(Layout("features").Ratio(2), Layout("stories").Ratio(3))
        |> ignore

        left["features"].Update(panel "[bold deepskyblue1]Features[/]" (featuresTable snapshot))
        |> ignore

        left["stories"].Update(panel "[bold green]User Stories[/]" (storiesTable snapshot))
        |> ignore

        let right = Layout("right").Ratio(4)

        right.SplitRows(Layout("plan").Ratio(2), Layout("tasks").Ratio(3), Layout("detail").Ratio(2))
        |> ignore

        right["plan"].Update(panel "[bold purple]Plan[/]" (planRenderable snapshot))
        |> ignore

        right["tasks"].Update(panel "[bold yellow]Tasks[/]" (tasksTable snapshot))
        |> ignore

        right["detail"].Update(panel "[bold white]Task Detail[/]" (detailRenderable snapshot))
        |> ignore

        let resolvedLayout = Domain.resolveLayout width snapshot.Ui.Layout

        let main = Layout("main")

        match snapshot.FullScreen with
        | Some modal -> main.Update(fullScreenRenderable snapshot modal) |> ignore
        | None ->
            match resolvedLayout with
            | WidescreenLayout -> main.SplitColumns(left, right) |> ignore
            | VerticalLayout ->
                main.SplitRows(
                    Layout("features").Ratio(2),
                    Layout("stories").Ratio(2),
                    Layout("plan").Ratio(2),
                    Layout("tasks").Ratio(3),
                    Layout("detail").Ratio(2)
                )
                |> ignore

                main["features"]
                    .Update(panel (sprintf "[bold %s]Features[/]" (color PanelAccent ui)) (featuresTable snapshot))
                |> ignore

                main["stories"].Update(panel "[bold green]User Stories[/]" (storiesTable snapshot))
                |> ignore

                main["plan"].Update(panel "[bold purple]Plan[/]" (planRenderable snapshot))
                |> ignore

                main["tasks"].Update(panel "[bold yellow]Tasks[/]" (tasksTable snapshot))
                |> ignore

                main["detail"].Update(panel "[bold white]Task Detail[/]" (detailRenderable snapshot))
                |> ignore

        let footer =
            let actions =
                if List.isEmpty snapshot.Features then
                    "[bold]C[/] constitution  [bold]L[/] checklists  [bold]r[/] refresh  [bold],[/] settings  [bold]q[/] quit  [grey]create specs with Speckit to begin[/]"
                else
                    "[bold]j/k[/] features  [bold]up/down[/] stories  [bold]left/right[/] tasks  [bold]h/l[/] table scroll  [bold]F/S/P/T[/] full screen  [bold]C[/] constitution  [bold]L[/] checklists  [bold]arrows[/] scroll detail  [bold],[/] settings  [bold]r[/] refresh  [bold]q[/] quit"

            let rows =
                Rows(
                    [| Rule("[grey]controls[/]") :> IRenderable
                       Markup(actions) :> IRenderable
                       diagnosticsRenderable snapshot :> IRenderable |]
                )

            panel "[bold grey]Status[/]" rows

        let root = Layout("root")

        root.SplitRows(Layout("header").Size(3), Layout("main"), Layout("footer").Size(12))
        |> ignore

        root["header"].Update(header) |> ignore
        root["main"].Update(main) |> ignore
        root["footer"].Update(footer) |> ignore
        root :> IRenderable

    let snapshotRenderable snapshot : IRenderable =
        let width =
            try
                Console.WindowWidth
            with _ ->
                120

        snapshotRenderableForWidth width snapshot

    let renderSnapshot snapshot =
        AnsiConsole.Write(snapshotRenderable snapshot)
