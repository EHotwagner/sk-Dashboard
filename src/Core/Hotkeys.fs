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
        | "refresh" -> Some Refresh
        | "hotkeys.reload" -> Some HotkeysReload
        | "quit" -> Some Quit
        | _ -> None

    let defaultBindings =
        [ { Command = FeaturePrevious; KeySequence = "k"; Scope = "dashboard"; Source = "default" }
          { Command = FeatureNext; KeySequence = "j"; Scope = "dashboard"; Source = "default" }
          { Command = FeatureCheckout; KeySequence = "enter"; Scope = "dashboard"; Source = "default" }
          { Command = StoryPrevious; KeySequence = "up"; Scope = "dashboard"; Source = "default" }
          { Command = StoryNext; KeySequence = "down"; Scope = "dashboard"; Source = "default" }
          { Command = TaskPrevious; KeySequence = "left"; Scope = "dashboard"; Source = "default" }
          { Command = TaskNext; KeySequence = "right"; Scope = "dashboard"; Source = "default" }
          { Command = PaneNext; KeySequence = "tab"; Scope = "dashboard"; Source = "default" }
          { Command = PanePrevious; KeySequence = "shift+tab"; Scope = "dashboard"; Source = "default" }
          { Command = DetailsOpen; KeySequence = "d"; Scope = "dashboard"; Source = "default" }
          { Command = DetailsClose; KeySequence = "esc"; Scope = "dashboard"; Source = "default" }
          { Command = Refresh; KeySequence = "r"; Scope = "dashboard"; Source = "default" }
          { Command = HotkeysReload; KeySequence = "R"; Scope = "dashboard"; Source = "default" }
          { Command = Quit; KeySequence = "q"; Scope = "dashboard"; Source = "default" } ]

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
        let userByCommand = userBindings |> List.map (fun binding -> binding.Command, binding) |> Map.ofList

        defaultBindings
        |> List.map (fun binding -> Map.tryFind binding.Command userByCommand |> Option.defaultValue binding)

    let colorRoleDefaults () =
        Domain.defaultUiPreferences.Colors

    let parseLayoutMode (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "auto" -> Some Auto
        | "widescreen" -> Some Widescreen
        | "vertical" -> Some Vertical
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
            match Int32.TryParse(hex, Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture) with
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
        tryHexRgb text |> Option.orElse (knownNamedColors |> Map.tryFind (text.ToLowerInvariant()))

    let validColor (value: string) =
        let text = value.Trim()
        validColorName text || Option.isSome (tryHexRgb text)

    let luminance (r, g, b) =
        let channel value =
            let c = float value / 255.0
            if c <= 0.03928 then c / 12.92 else Math.Pow((c + 0.055) / 1.055, 2.4)

        0.2126 * channel r + 0.7152 * channel g + 0.0722 * channel b

    let contrastRatio foreground background =
        let a = luminance foreground
        let b = luminance background
        let light = max a b
        let dark = min a b
        (light + 0.05) / (dark + 0.05)

    let source (path: string) =
        Some { Path = path; Line = None }

    let diagnostic (path: string) severity message =
        Domain.diagnostic severity message (source path)

    let readStringProperty (element: JsonElement) (name: string) =
        match element.TryGetProperty(name) with
        | true, value when value.ValueKind = JsonValueKind.String ->
            value.GetString() |> Option.ofObj
        | _ -> None

    let parseBindings (path: string) (root: JsonElement) =
        let bindingsElement =
            match root.TryGetProperty("bindings") with
            | true, value when value.ValueKind = JsonValueKind.Array -> Some value
            | _ -> None

        match bindingsElement with
        | None -> defaultBindings, [ diagnostic path DiagnosticSeverity.Error "Hotkey config must contain a bindings array." ]
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
                        diagnostic path DiagnosticSeverity.Error ("Unknown hotkey command: " + (commandText |> Option.defaultValue "(missing)"))
                        :: diagnostics
                | _, _ ->
                    diagnostics <- diagnostic path DiagnosticSeverity.Error "Hotkey binding is missing key." :: diagnostics

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
                    | Choice1Of2 foreground -> Choice1Of2 { Foreground = foreground; Background = None }
                    | Choice2Of2 diagnostic -> Choice2Of2 diagnostic
            | JsonValueKind.Object ->
                match readStringProperty element "foreground", readStringProperty element "background" with
                | Some foreground, Some background ->
                    match validatePart "foreground" foreground, validatePart "background" background with
                    | Choice1Of2 fg, Choice1Of2 bg -> Choice1Of2 { Foreground = fg; Background = Some bg }
                    | Choice2Of2 diagnostic, _
                    | _, Choice2Of2 diagnostic -> Choice2Of2 diagnostic
                | _ -> invalid (sprintf "Color role %s must include foreground and background strings." (Domain.colorRoleId role))
            | _ -> invalid (sprintf "Color role %s must be a string or foreground/background object." (Domain.colorRoleId role))

        match parsed with
        | Choice1Of2 style ->
            match style.Background |> Option.bind colorRgb, colorRgb style.Foreground with
            | Some background, Some foreground when contrastRatio foreground background < 4.5 ->
                Choice2Of2(Domain.diagnostic Warning (sprintf "Low-contrast color pair for %s; using default colors." (Domain.colorRoleId role)) (source path))
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
                    | None -> diagnostics <- diagnostic path Warning ("Unknown color role ignored: " + property.Name) :: diagnostics
                    | Some role ->
                        match parseColorStyle path role property.Value with
                        | Choice1Of2 style -> colors <- Map.add role style colors
                        | Choice2Of2 d -> diagnostics <- d :: diagnostics
            | true, _ -> diagnostics <- diagnostic path Warning "UI colors must be an object; using default colors." :: diagnostics
            | false, _ -> ()

            { Layout = layout; Colors = colors }, List.rev diagnostics

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

                { Bindings = bindings
                  Ui = ui
                  Diagnostics = bindingDiagnostics @ uiDiagnostics }
            with ex ->
                { Bindings = defaultBindings
                  Ui = Domain.defaultUiPreferences
                  Diagnostics = [ diagnostic path DiagnosticSeverity.Error ("Dashboard preferences could not be loaded: " + ex.Message) ] }

    let loadBindings path =
        let preferences = loadPreferences path
        preferences.Bindings, preferences.Diagnostics
