namespace SkDashboard.Core

open System
open System.IO
open System.Text.Json

type DashboardCommand =
    | FeaturePrevious
    | FeatureNext
    | FeatureCheckout
    | StoryPrevious
    | StoryNext
    | TaskPrevious
    | TaskNext
    | PaneNext
    | PanePrevious
    | DetailsOpen
    | DetailsClose
    | FullScreenFeature
    | FullScreenStory
    | FullScreenPlan
    | FullScreenTask
    | ConstitutionOpen
    | ChecklistOpen
    | TableScrollLeft
    | TableScrollRight
    | DetailScrollUp
    | DetailScrollDown
    | DetailScrollLeft
    | DetailScrollRight
    | SettingsOpen
    | SettingsSave
    | SettingsDiscard
    | SettingsReload
    | SettingsOverwrite
    | SettingsAppThemePrevious
    | SettingsAppThemeNext
    | SettingsMarkdownThemePrevious
    | SettingsMarkdownThemeNext
    | Refresh
    | HotkeysReload
    | Quit

type HotkeyBinding =
    { Command: DashboardCommand
      KeySequence: string
      Scope: string
      Source: string }

type DashboardPreferences =
    { Bindings: HotkeyBinding list
      Ui: DashboardUiPreferences
      Diagnostics: Diagnostic list }

module Hotkeys =
    let commandId command =
        match command with
        | FeaturePrevious -> "feature.previous"
        | FeatureNext -> "feature.next"
        | FeatureCheckout -> "feature.checkout"
        | StoryPrevious -> "story.previous"
        | StoryNext -> "story.next"
        | TaskPrevious -> "task.previous"
        | TaskNext -> "task.next"
        | PaneNext -> "pane.next"
        | PanePrevious -> "pane.previous"
        | DetailsOpen -> "details.open"
        | DetailsClose -> "details.close"
        | FullScreenFeature -> "fullscreen.feature"
        | FullScreenStory -> "fullscreen.story"
        | FullScreenPlan -> "fullscreen.plan"
        | FullScreenTask -> "fullscreen.task"
        | ConstitutionOpen -> "constitution.open"
        | ChecklistOpen -> "checklists.open"
        | TableScrollLeft -> "table.scrollLeft"
        | TableScrollRight -> "table.scrollRight"
        | DetailScrollUp -> "detail.scrollUp"
        | DetailScrollDown -> "detail.scrollDown"
        | DetailScrollLeft -> "detail.scrollLeft"
        | DetailScrollRight -> "detail.scrollRight"
        | SettingsOpen -> "settings.open"
        | SettingsSave -> "settings.save"
        | SettingsDiscard -> "settings.discard"
        | SettingsReload -> "settings.reload"
        | SettingsOverwrite -> "settings.overwrite"
        | SettingsAppThemePrevious -> "settings.appTheme.previous"
        | SettingsAppThemeNext -> "settings.appTheme.next"
        | SettingsMarkdownThemePrevious -> "settings.markdownTheme.previous"
        | SettingsMarkdownThemeNext -> "settings.markdownTheme.next"
        | Refresh -> "refresh"
        | HotkeysReload -> "hotkeys.reload"
        | Quit -> "quit"

    let commandFromId command =
        match command with
        | "feature.previous" -> Some FeaturePrevious
        | "feature.next" -> Some FeatureNext
        | "feature.checkout" -> Some FeatureCheckout
        | "story.previous" -> Some StoryPrevious
        | "story.next" -> Some StoryNext
        | "task.previous" -> Some TaskPrevious
        | "task.next" -> Some TaskNext
        | "pane.next" -> Some PaneNext
        | "pane.previous" -> Some PanePrevious
        | "details.open" -> Some DetailsOpen
        | "details.close" -> Some DetailsClose
        | "fullscreen.feature" -> Some FullScreenFeature
        | "fullscreen.story" -> Some FullScreenStory
        | "fullscreen.plan" -> Some FullScreenPlan
        | "fullscreen.task" -> Some FullScreenTask
        | "constitution.open" -> Some ConstitutionOpen
        | "checklists.open" -> Some ChecklistOpen
        | "table.scrollLeft" -> Some TableScrollLeft
        | "table.scrollRight" -> Some TableScrollRight
        | "detail.scrollUp" -> Some DetailScrollUp
        | "detail.scrollDown" -> Some DetailScrollDown
        | "detail.scrollLeft" -> Some DetailScrollLeft
        | "detail.scrollRight" -> Some DetailScrollRight
        | "settings.open" -> Some SettingsOpen
        | "settings.save" -> Some SettingsSave
        | "settings.discard" -> Some SettingsDiscard
        | "settings.reload" -> Some SettingsReload
        | "settings.overwrite" -> Some SettingsOverwrite
        | "settings.appTheme.previous" -> Some SettingsAppThemePrevious
        | "settings.appTheme.next" -> Some SettingsAppThemeNext
        | "settings.markdownTheme.previous" -> Some SettingsMarkdownThemePrevious
        | "settings.markdownTheme.next" -> Some SettingsMarkdownThemeNext
        | "refresh" -> Some Refresh
        | "hotkeys.reload" -> Some HotkeysReload
        | "quit" -> Some Quit
        | _ -> None

    let commandLabel command =
        match command with
        | FeaturePrevious -> "Previous feature"
        | FeatureNext -> "Next feature"
        | FeatureCheckout -> "Checkout feature"
        | StoryPrevious -> "Previous story"
        | StoryNext -> "Next story"
        | TaskPrevious -> "Previous task"
        | TaskNext -> "Next task"
        | PaneNext -> "Next pane"
        | PanePrevious -> "Previous pane"
        | DetailsOpen -> "Open details"
        | DetailsClose -> "Close details"
        | FullScreenFeature -> "Open feature document"
        | FullScreenStory -> "Open story document"
        | FullScreenPlan -> "Open plan document"
        | FullScreenTask -> "Open task document"
        | ConstitutionOpen -> "Open constitution"
        | ChecklistOpen -> "Open checklists"
        | TableScrollLeft -> "Scroll table left"
        | TableScrollRight -> "Scroll table right"
        | DetailScrollUp -> "Scroll detail up"
        | DetailScrollDown -> "Scroll detail down"
        | DetailScrollLeft -> "Scroll detail left"
        | DetailScrollRight -> "Scroll detail right"
        | SettingsOpen -> "Open settings"
        | SettingsSave -> "Save settings"
        | SettingsDiscard -> "Discard settings"
        | SettingsReload -> "Reload settings"
        | SettingsOverwrite -> "Overwrite settings"
        | SettingsAppThemePrevious -> "Previous app theme"
        | SettingsAppThemeNext -> "Next app theme"
        | SettingsMarkdownThemePrevious -> "Previous Markdown theme"
        | SettingsMarkdownThemeNext -> "Next Markdown theme"
        | Refresh -> "Refresh"
        | HotkeysReload -> "Reload hotkeys"
        | Quit -> "Quit"

    let defaultBindings =
        [ { Command = FeaturePrevious
            KeySequence = "k"
            Scope = "dashboard"
            Source = "default" }
          { Command = FeatureNext
            KeySequence = "j"
            Scope = "dashboard"
            Source = "default" }
          { Command = FeatureCheckout
            KeySequence = "enter"
            Scope = "dashboard"
            Source = "default" }
          { Command = StoryPrevious
            KeySequence = "up"
            Scope = "dashboard"
            Source = "default" }
          { Command = StoryNext
            KeySequence = "down"
            Scope = "dashboard"
            Source = "default" }
          { Command = TaskPrevious
            KeySequence = "left"
            Scope = "dashboard"
            Source = "default" }
          { Command = TaskNext
            KeySequence = "right"
            Scope = "dashboard"
            Source = "default" }
          { Command = PaneNext
            KeySequence = "tab"
            Scope = "dashboard"
            Source = "default" }
          { Command = PanePrevious
            KeySequence = "shift+tab"
            Scope = "dashboard"
            Source = "default" }
          { Command = DetailsOpen
            KeySequence = "d"
            Scope = "dashboard"
            Source = "default" }
          { Command = DetailsClose
            KeySequence = "esc"
            Scope = "dashboard"
            Source = "default" }
          { Command = FullScreenFeature
            KeySequence = "F"
            Scope = "dashboard"
            Source = "default" }
          { Command = FullScreenStory
            KeySequence = "S"
            Scope = "dashboard"
            Source = "default" }
          { Command = FullScreenPlan
            KeySequence = "P"
            Scope = "dashboard"
            Source = "default" }
          { Command = FullScreenTask
            KeySequence = "T"
            Scope = "dashboard"
            Source = "default" }
          { Command = ConstitutionOpen
            KeySequence = "C"
            Scope = "dashboard"
            Source = "default" }
          { Command = ChecklistOpen
            KeySequence = "L"
            Scope = "dashboard"
            Source = "default" }
          { Command = TableScrollLeft
            KeySequence = "h"
            Scope = "dashboard"
            Source = "default" }
          { Command = TableScrollRight
            KeySequence = "l"
            Scope = "dashboard"
            Source = "default" }
          { Command = DetailScrollUp
            KeySequence = "u"
            Scope = "dashboard"
            Source = "default" }
          { Command = DetailScrollDown
            KeySequence = "v"
            Scope = "dashboard"
            Source = "default" }
          { Command = DetailScrollLeft
            KeySequence = "shift+left"
            Scope = "dashboard"
            Source = "default" }
          { Command = DetailScrollRight
            KeySequence = "shift+right"
            Scope = "dashboard"
            Source = "default" }
          { Command = SettingsOpen
            KeySequence = ","
            Scope = "dashboard"
            Source = "default" }
          { Command = SettingsSave
            KeySequence = "1"
            Scope = "settings"
            Source = "default" }
          { Command = SettingsDiscard
            KeySequence = "2"
            Scope = "settings"
            Source = "default" }
          { Command = SettingsReload
            KeySequence = "3"
            Scope = "settings"
            Source = "default" }
          { Command = SettingsOverwrite
            KeySequence = "4"
            Scope = "settings"
            Source = "default" }
          { Command = SettingsAppThemePrevious
            KeySequence = "a"
            Scope = "settings"
            Source = "default" }
          { Command = SettingsAppThemeNext
            KeySequence = "A"
            Scope = "settings"
            Source = "default" }
          { Command = SettingsMarkdownThemePrevious
            KeySequence = "m"
            Scope = "settings"
            Source = "default" }
          { Command = SettingsMarkdownThemeNext
            KeySequence = "M"
            Scope = "settings"
            Source = "default" }
          { Command = Refresh
            KeySequence = "r"
            Scope = "dashboard"
            Source = "default" }
          { Command = HotkeysReload
            KeySequence = "R"
            Scope = "dashboard"
            Source = "default" }
          { Command = Quit
            KeySequence = "q"
            Scope = "dashboard"
            Source = "default" } ]

    let supportedKey key =
        let known =
            Set.ofList
                [ "enter"
                  "esc"
                  "tab"
                  "shift+tab"
                  "up"
                  "down"
                  "left"
                  "right"
                  "shift+left"
                  "shift+right" ]

        not (String.IsNullOrWhiteSpace key)
        && (key.Length = 1 || Set.contains (key.ToLowerInvariant()) known)
        && not (key.Contains " ")

    let validateBindings bindings =
        let unsupported =
            bindings
            |> List.choose (fun binding ->
                if supportedKey binding.KeySequence then
                    None
                else
                    Some(Domain.diagnostic Error ("Unsupported hotkey sequence: " + binding.KeySequence) None))

        let conflicts =
            bindings
            |> List.groupBy _.KeySequence
            |> List.choose (fun (key, collisions) ->
                if List.length collisions > 1 then
                    Some(Domain.diagnostic Error ("Conflicting hotkey binding: " + key) None)
                else
                    None)

        unsupported @ conflicts

    let mergeBindings userBindings =
        let userByCommand =
            userBindings |> List.map (fun binding -> binding.Command, binding) |> Map.ofList

        defaultBindings
        |> List.map (fun binding -> Map.tryFind binding.Command userByCommand |> Option.defaultValue binding)

    let colorRoleDefaults () = Domain.defaultUiPreferences.Colors

    let parseLayoutMode (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "auto" -> Some Auto
        | "widescreen" -> Some Widescreen
        | "vertical" -> Some Vertical
        | _ -> None

    let readBoolProperty (element: JsonElement) (name: string) =
        match element.TryGetProperty(name) with
        | true, value when value.ValueKind = JsonValueKind.True -> Some true
        | true, value when value.ValueKind = JsonValueKind.False -> Some false
        | _ -> None

    let readIntProperty (element: JsonElement) (name: string) =
        match element.TryGetProperty(name) with
        | true, value when value.ValueKind = JsonValueKind.Number ->
            match value.TryGetInt32() with
            | true, parsed -> Some parsed
            | _ -> None
        | _ -> None

    let knownNamedColors =
        [ "black", (0, 0, 0)
          "white", (255, 255, 255)
          "grey", (128, 128, 128)
          "gray", (128, 128, 128)
          "grey7", (18, 18, 18)
          "gray7", (18, 18, 18)
          "grey23", (59, 59, 59)
          "gray23", (59, 59, 59)
          "grey42", (107, 107, 107)
          "gray42", (107, 107, 107)
          "green", (0, 255, 0)
          "yellow", (255, 255, 0)
          "red", (255, 0, 0)
          "blue", (0, 0, 255)
          "cyan", (0, 255, 255)
          "magenta", (255, 0, 255)
          "purple", (128, 0, 128)
          "deepskyblue1", (0, 191, 255) ]
        |> Map.ofList

    let validColorName (value: string) =
        knownNamedColors |> Map.containsKey (value.Trim().ToLowerInvariant())

    let tryHexRgb (value: string) =
        let text = value.Trim()

        if text.Length = 7 && text[0] = '#' then
            let hex = text.Substring 1

            match
                Int32.TryParse(hex, Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture)
            with
            | true, rgb ->
                let r = (rgb >>> 16) &&& 0xff
                let g = (rgb >>> 8) &&& 0xff
                let b = rgb &&& 0xff
                Some(r, g, b)
            | _ -> None
        else
            None

    let colorRgb (value: string) =
        let text = value.Trim()

        tryHexRgb text
        |> Option.orElse (knownNamedColors |> Map.tryFind (text.ToLowerInvariant()))

    let validColor (value: string) =
        let text = value.Trim()
        validColorName text || Option.isSome (tryHexRgb text)

    let luminance (r, g, b) =
        let channel value =
            let c = float value / 255.0

            if c <= 0.03928 then
                c / 12.92
            else
                Math.Pow((c + 0.055) / 1.055, 2.4)

        0.2126 * channel r + 0.7152 * channel g + 0.0722 * channel b

    let contrastRatio foreground background =
        let a = luminance foreground
        let b = luminance background
        let light = max a b
        let dark = min a b
        (light + 0.05) / (dark + 0.05)

    let source (path: string) = Some { Path = path; Line = None }

    let diagnostic (path: string) severity message =
        Domain.diagnostic severity message (source path)

    let readStringProperty (element: JsonElement) (name: string) =
        match element.TryGetProperty(name) with
        | true, value when value.ValueKind = JsonValueKind.String -> value.GetString() |> Option.ofObj
        | _ -> None

    let parseBindings (path: string) (root: JsonElement) =
        let bindingsElement =
            match root.TryGetProperty("bindings") with
            | true, value when value.ValueKind = JsonValueKind.Array -> Some value
            | _ -> None

        match bindingsElement with
        | None ->
            defaultBindings, [ diagnostic path DiagnosticSeverity.Error "Hotkey config must contain a bindings array." ]
        | Some bindingsJson ->
            let mutable diagnostics = []
            let mutable userBindings = []

            for item in bindingsJson.EnumerateArray() do
                let commandText = readStringProperty item "command"
                let keyText = readStringProperty item "key"

                match commandText |> Option.bind commandFromId, keyText with
                | Some command, Some key ->
                    userBindings <-
                        { Command = command
                          KeySequence = key
                          Scope = "dashboard"
                          Source = path }
                        :: userBindings
                | None, _ ->
                    diagnostics <-
                        diagnostic
                            path
                            DiagnosticSeverity.Error
                            ("Unknown hotkey command: " + (commandText |> Option.defaultValue "(missing)"))
                        :: diagnostics
                | _, _ ->
                    diagnostics <-
                        diagnostic path DiagnosticSeverity.Error "Hotkey binding is missing key."
                        :: diagnostics

            let merged = mergeBindings (List.rev userBindings)
            let validationDiagnostics = validateBindings merged
            let allDiagnostics = List.rev diagnostics @ validationDiagnostics

            if List.isEmpty allDiagnostics then
                merged, []
            else
                defaultBindings, allDiagnostics

    let parseColorStyle (path: string) role (element: JsonElement) =
        let invalid message =
            Choice2Of2(Domain.diagnostic Warning message (source path))

        let validatePart label value =
            if validColor value then
                Choice1Of2 value
            else
                invalid (sprintf "Invalid color for %s %s: %s" (Domain.colorRoleId role) label value)

        let parsed =
            match element.ValueKind with
            | JsonValueKind.String ->
                match element.GetString() |> Option.ofObj with
                | None -> invalid (sprintf "Invalid color for %s: null" (Domain.colorRoleId role))
                | Some value ->
                    match validatePart "foreground" value with
                    | Choice1Of2 foreground ->
                        Choice1Of2
                            { Foreground = foreground
                              Background = None }
                    | Choice2Of2 diagnostic -> Choice2Of2 diagnostic
            | JsonValueKind.Object ->
                match readStringProperty element "foreground", readStringProperty element "background" with
                | Some foreground, Some background ->
                    match validatePart "foreground" foreground, validatePart "background" background with
                    | Choice1Of2 fg, Choice1Of2 bg ->
                        Choice1Of2
                            { Foreground = fg
                              Background = Some bg }
                    | Choice2Of2 diagnostic, _
                    | _, Choice2Of2 diagnostic -> Choice2Of2 diagnostic
                | _ ->
                    invalid (
                        sprintf
                            "Color role %s must include foreground and background strings."
                            (Domain.colorRoleId role)
                    )
            | _ ->
                invalid (
                    sprintf "Color role %s must be a string or foreground/background object." (Domain.colorRoleId role)
                )

        match parsed with
        | Choice1Of2 style ->
            match style.Background |> Option.bind colorRgb, colorRgb style.Foreground with
            | Some background, Some foreground when contrastRatio foreground background < 4.5 ->
                Choice2Of2(
                    Domain.diagnostic
                        Warning
                        (sprintf "Low-contrast color pair for %s; using default colors." (Domain.colorRoleId role))
                        (source path)
                )
            | _ -> Choice1Of2 style
        | Choice2Of2 diagnostic -> Choice2Of2 diagnostic

    let parseUi (path: string) (root: JsonElement) =
        match root.TryGetProperty("ui") with
        | false, _ -> Domain.defaultUiPreferences, []
        | true, ui when ui.ValueKind <> JsonValueKind.Object ->
            Domain.defaultUiPreferences, [ diagnostic path Warning "UI preferences must be an object; using defaults." ]
        | true, ui ->
            let mutable diagnostics = []
            let mutable layout = Domain.defaultUiPreferences.Layout
            let mutable table = Domain.defaultUiPreferences.Table
            let mutable detail = Domain.defaultUiPreferences.Detail
            let mutable liveReload = Domain.defaultUiPreferences.LiveReload
            let mutable themes = Domain.defaultUiPreferences.Themes
            let mutable colors = Domain.defaultUiPreferences.Colors

            match ui.TryGetProperty("layout") with
            | true, value when value.ValueKind = JsonValueKind.String ->
                match value.GetString() |> Option.ofObj |> Option.bind parseLayoutMode with
                | Some parsed -> layout <- parsed
                | None -> diagnostics <- diagnostic path Warning "Unsupported layout mode; using auto." :: diagnostics
            | true, _ -> diagnostics <- diagnostic path Warning "Unsupported layout mode; using auto." :: diagnostics
            | false, _ -> ()

            match ui.TryGetProperty("colors") with
            | true, value when value.ValueKind = JsonValueKind.Object ->
                for property in value.EnumerateObject() do
                    match Domain.tryColorRole property.Name with
                    | None ->
                        diagnostics <-
                            diagnostic path Warning ("Unknown color role ignored: " + property.Name)
                            :: diagnostics
                    | Some role ->
                        match parseColorStyle path role property.Value with
                        | Choice1Of2 style -> colors <- Map.add role style colors
                        | Choice2Of2 d -> diagnostics <- d :: diagnostics
            | true, _ ->
                diagnostics <-
                    diagnostic path Warning "UI colors must be an object; using default colors."
                    :: diagnostics
            | false, _ -> ()

            match ui.TryGetProperty("table") with
            | true, value when value.ValueKind = JsonValueKind.Object ->
                match readStringProperty value "border" |> Option.bind Domain.tryTableBorder with
                | Some border -> table <- { table with Border = border }
                | None when value.TryGetProperty("border") |> fst ->
                    diagnostics <-
                        diagnostic path Warning "Unsupported table border; using rounded."
                        :: diagnostics
                | None -> ()

                match readIntProperty value "stickyColumns" with
                | Some count when count >= 0 -> table <- { table with StickyColumns = count }
                | Some _ ->
                    diagnostics <-
                        diagnostic path Warning "Table stickyColumns must be non-negative; using default."
                        :: diagnostics
                | None -> ()

                match readIntProperty value "horizontalStep" with
                | Some step when step > 0 -> table <- { table with HorizontalStep = step }
                | Some _ ->
                    diagnostics <-
                        diagnostic path Warning "Table horizontalStep must be positive; using default."
                        :: diagnostics
                | None -> ()

                match readBoolProperty value "alternateRowShading" with
                | Some enabled -> table <- { table with AlternateRowShading = enabled }
                | None -> ()
            | true, _ ->
                diagnostics <-
                    diagnostic path Warning "UI table preferences must be an object; using default table settings."
                    :: diagnostics
            | false, _ -> ()

            match ui.TryGetProperty("detail") with
            | true, value when value.ValueKind = JsonValueKind.Object ->
                match readBoolProperty value "wrapText" with
                | Some wrap -> detail <- { detail with WrapText = wrap }
                | None -> ()

                match readIntProperty value "horizontalStep" with
                | Some step when step > 0 -> detail <- { detail with HorizontalStep = step }
                | Some _ ->
                    diagnostics <-
                        diagnostic path Warning "Detail horizontalStep must be positive; using default."
                        :: diagnostics
                | None -> ()
            | true, _ ->
                diagnostics <-
                    diagnostic path Warning "UI detail preferences must be an object; using default detail settings."
                    :: diagnostics
            | false, _ -> ()

            match ui.TryGetProperty("liveReload") with
            | true, value when value.ValueKind = JsonValueKind.Object ->
                match readBoolProperty value "enabled" with
                | Some enabled -> liveReload <- { liveReload with Enabled = enabled }
                | None -> ()

                match readIntProperty value "debounceMilliseconds" with
                | Some ms when ms >= 50 && ms <= 2000 ->
                    liveReload <-
                        { liveReload with
                            DebounceMilliseconds = ms }
                | Some _ ->
                    diagnostics <-
                        diagnostic
                            path
                            Warning
                            "Live reload debounceMilliseconds must be between 50 and 2000; using default."
                        :: diagnostics
                | None -> ()
            | true, _ ->
                diagnostics <-
                    diagnostic
                        path
                        Warning
                        "UI liveReload preferences must be an object; using default live reload settings."
                    :: diagnostics
            | false, _ -> ()

            match ui.TryGetProperty("themes") with
            | true, value when value.ValueKind = JsonValueKind.Object ->
                let appTheme =
                    readStringProperty value "app"
                    |> Option.orElse (readStringProperty value "selectedAppThemeId")
                    |> Option.defaultValue themes.SelectedAppThemeId

                let markdownTheme =
                    readStringProperty value "markdown"
                    |> Option.orElse (readStringProperty value "selectedMarkdownThemeId")
                    |> Option.defaultValue themes.SelectedMarkdownThemeId

                themes <-
                    { themes with
                        SelectedAppThemeId =
                            if String.IsNullOrWhiteSpace appTheme then
                                Domain.defaultThemeSelection.SelectedAppThemeId
                            else
                                appTheme
                        SelectedMarkdownThemeId =
                            if String.IsNullOrWhiteSpace markdownTheme then
                                Domain.defaultThemeSelection.SelectedMarkdownThemeId
                            else
                                markdownTheme }
            | true, _ ->
                diagnostics <-
                    diagnostic path Warning "UI themes must be an object; using default theme selections."
                    :: diagnostics
            | false, _ -> ()

            { Layout = layout
              Table = table
              Detail = detail
              LiveReload = liveReload
              Themes = themes
              Markdown = Domain.defaultUiPreferences.Markdown
              Colors = colors },
            List.rev diagnostics

    let currentConfigVersion path =
        if String.IsNullOrWhiteSpace path || not (File.Exists path) then
            { Path = path
              LastWriteTimeUtc = None
              Length = None }
        else
            let info = FileInfo path

            { Path = path
              LastWriteTimeUtc = Some(DateTimeOffset info.LastWriteTimeUtc)
              Length = Some info.Length }

    let themeFeedbackDiagnostic feedback =
        Domain.diagnostic feedback.Severity feedback.Message (feedback.Source |> Option.map (fun path -> { Path = path; Line = None }))

    let resolveUiThemes path (ui: DashboardUiPreferences) =
        let catalog = SpeckitArtifacts.discoverThemeCatalog path

        let appTheme =
            catalog.AppThemes
            |> List.tryFind (fun theme -> theme.Id = ui.Themes.SelectedAppThemeId)

        let markdownTheme =
            catalog.MarkdownThemes
            |> List.tryFind (fun theme -> theme.Id = ui.Themes.SelectedMarkdownThemeId)

        let fallbackDiagnostics =
            match appTheme with
            | Some _ -> []
            | None ->
                [ Domain.diagnostic
                      Warning
                      ("Selected app theme is unavailable; using default: " + ui.Themes.SelectedAppThemeId)
                      None ]

        let markdownFallbackDiagnostics =
            match markdownTheme with
            | Some _ -> []
            | None ->
                [ Domain.diagnostic
                      Warning
                      ("Selected Markdown theme is unavailable; using default: " + ui.Themes.SelectedMarkdownThemeId)
                      None ]

        let resolvedApp =
            appTheme
            |> Option.orElse (catalog.AppThemes |> List.tryFind (fun theme -> theme.Id = "default"))

        let resolvedMarkdown =
            markdownTheme
            |> Option.orElse (catalog.MarkdownThemes |> List.tryFind (fun theme -> theme.Id = "default"))

        let themeSelection =
            { ui.Themes with
                AvailableAppThemes = catalog.AppThemes |> List.map _.Id
                AvailableMarkdownThemes = catalog.MarkdownThemes |> List.map _.Id
                AppThemeFallback = Option.isNone appTheme
                MarkdownThemeFallback = Option.isNone markdownTheme }

        let markdownPresentation =
            resolvedMarkdown
            |> Option.map (fun theme ->
                { Id = theme.Id
                  DisplayName = theme.DisplayName
                  Colors = theme.Colors
                  Spacing = theme.Spacing })
            |> Option.defaultValue Domain.defaultUiPreferences.Markdown

        let themedUi =
            match resolvedApp with
            | None ->
                { ui with
                    Themes = themeSelection
                    Markdown = markdownPresentation }
            | Some theme ->
                let colors =
                    theme.Colors
                    |> Map.fold
                        (fun resolved role themeStyle ->
                            let current = ui.Colors |> Map.tryFind role
                            let defaultStyle = Domain.defaultUiPreferences.Colors |> Map.tryFind role

                            match current, defaultStyle with
                            | Some currentStyle, Some baseStyle when currentStyle <> baseStyle ->
                                Map.add role currentStyle resolved
                            | _ -> Map.add role themeStyle resolved)
                        theme.Colors

                { ui with
                    Themes = themeSelection
                    Markdown = markdownPresentation
                    Table =
                        if ui.Table = Domain.defaultUiPreferences.Table then
                            theme.Table
                        else
                            ui.Table
                    Colors = colors }

        let diagnostics =
            (catalog.Diagnostics |> List.map themeFeedbackDiagnostic)
            @ fallbackDiagnostics
            @ markdownFallbackDiagnostics

        themedUi, diagnostics

    let writePreferences (path: string) preferences =
        let directory = Path.GetDirectoryName path |> Option.ofObj

        match directory with
        | Some value when not (String.IsNullOrWhiteSpace value) -> Directory.CreateDirectory value |> ignore
        | _ -> ()

        let border = Domain.tableBorderId preferences.Ui.Table.Border

        let colorEntries =
            preferences.Ui.Colors
            |> Seq.map (fun kv ->
                let role = Domain.colorRoleId kv.Key
                let style = kv.Value

                match style.Background with
                | None -> sprintf "\"%s\":\"%s\"" role style.Foreground
                | Some background ->
                    sprintf "\"%s\":{\"foreground\":\"%s\",\"background\":\"%s\"}" role style.Foreground background)
            |> String.concat ","

        let bindings =
            preferences.Bindings
            |> List.map (fun binding ->
                sprintf "{\"command\":\"%s\",\"key\":\"%s\"}" (commandId binding.Command) binding.KeySequence)
            |> String.concat ","

        let json =
            sprintf
                """{"version":1,"bindings":[%s],"ui":{"layout":"%s","table":{"border":"%s","stickyColumns":%d,"horizontalStep":%d,"alternateRowShading":%s},"detail":{"wrapText":%s,"horizontalStep":%d},"liveReload":{"enabled":%s,"debounceMilliseconds":%d},"themes":{"app":"%s","markdown":"%s"},"colors":{%s}}}"""
                bindings
                (match preferences.Ui.Layout with
                 | Auto -> "auto"
                 | Widescreen -> "widescreen"
                 | Vertical -> "vertical")
                border
                preferences.Ui.Table.StickyColumns
                preferences.Ui.Table.HorizontalStep
                (if preferences.Ui.Table.AlternateRowShading then
                     "true"
                 else
                     "false")
                (if preferences.Ui.Detail.WrapText then "true" else "false")
                preferences.Ui.Detail.HorizontalStep
                (if preferences.Ui.LiveReload.Enabled then
                     "true"
                 else
                     "false")
                preferences.Ui.LiveReload.DebounceMilliseconds
                preferences.Ui.Themes.SelectedAppThemeId
                preferences.Ui.Themes.SelectedMarkdownThemeId
                colorEntries

        File.WriteAllText(path, json)

    let loadPreferences path =
        if String.IsNullOrWhiteSpace path || not (File.Exists path) then
            { Bindings = defaultBindings
              Ui = Domain.defaultUiPreferences
              Diagnostics = [] }
        else
            try
                use document = JsonDocument.Parse(File.ReadAllText path)
                let root = document.RootElement
                let bindings, bindingDiagnostics = parseBindings path root
                let ui, uiDiagnostics = parseUi path root
                let themedUi, themeDiagnostics = resolveUiThemes path ui

                { Bindings = bindings
                  Ui = themedUi
                  Diagnostics = bindingDiagnostics @ uiDiagnostics @ themeDiagnostics }
            with ex ->
                { Bindings = defaultBindings
                  Ui = Domain.defaultUiPreferences
                  Diagnostics =
                    [ diagnostic
                          path
                          DiagnosticSeverity.Error
                          ("Dashboard preferences could not be loaded: " + ex.Message) ] }

    let loadBindings path =
        let preferences = loadPreferences path
        preferences.Bindings, preferences.Diagnostics
