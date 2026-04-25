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
        [ { Command = FeaturePrevious; KeySequence = "K"; Scope = "dashboard"; Source = "default" }
          { Command = FeatureNext; KeySequence = "J"; Scope = "dashboard"; Source = "default" }
          { Command = FeatureCheckout; KeySequence = "enter"; Scope = "dashboard"; Source = "default" }
          { Command = StoryPrevious; KeySequence = "k"; Scope = "dashboard"; Source = "default" }
          { Command = StoryNext; KeySequence = "j"; Scope = "dashboard"; Source = "default" }
          { Command = TaskPrevious; KeySequence = "h"; Scope = "dashboard"; Source = "default" }
          { Command = TaskNext; KeySequence = "l"; Scope = "dashboard"; Source = "default" }
          { Command = PaneNext; KeySequence = "tab"; Scope = "dashboard"; Source = "default" }
          { Command = PanePrevious; KeySequence = "shift+tab"; Scope = "dashboard"; Source = "default" }
          { Command = DetailsOpen; KeySequence = "d"; Scope = "dashboard"; Source = "default" }
          { Command = DetailsClose; KeySequence = "esc"; Scope = "dashboard"; Source = "default" }
          { Command = Refresh; KeySequence = "r"; Scope = "dashboard"; Source = "default" }
          { Command = HotkeysReload; KeySequence = "R"; Scope = "dashboard"; Source = "default" }
          { Command = Quit; KeySequence = "q"; Scope = "dashboard"; Source = "default" } ]

    let supportedKey key =
        let known = Set.ofList [ "enter"; "esc"; "tab"; "shift+tab" ]

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

    let loadBindings path =
        if String.IsNullOrWhiteSpace path || not (File.Exists path) then
            defaultBindings, []
        else
            try
                use document = JsonDocument.Parse(File.ReadAllText path)
                let root = document.RootElement

                let bindingsElement =
                    match root.TryGetProperty("bindings") with
                    | true, value when value.ValueKind = JsonValueKind.Array -> Some value
                    | _ -> None

                match bindingsElement with
                | None -> defaultBindings, [ Domain.diagnostic Error "Hotkey config must contain a bindings array." (Some { Path = path; Line = None }) ]
                | Some bindingsJson ->
                    let mutable diagnostics = []
                    let mutable userBindings = []

                    for item in bindingsJson.EnumerateArray() do
                        let commandText: string option =
                            match item.TryGetProperty("command") with
                            | true, value when value.ValueKind = JsonValueKind.String ->
                                match value.GetString() with
                                | null -> None
                                | text -> Some text
                            | _ -> None

                        let keyText: string option =
                            match item.TryGetProperty("key") with
                            | true, value when value.ValueKind = JsonValueKind.String ->
                                match value.GetString() with
                                | null -> None
                                | text -> Some text
                            | _ -> None

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
                                Domain.diagnostic Error ("Unknown hotkey command: " + (commandText |> Option.defaultValue "(missing)")) (Some { Path = path; Line = None })
                                :: diagnostics
                        | _, _ ->
                            diagnostics <- Domain.diagnostic Error "Hotkey binding is missing key." (Some { Path = path; Line = None }) :: diagnostics

                    let merged = mergeBindings (List.rev userBindings)
                    let validationDiagnostics = validateBindings merged
                    let allDiagnostics = List.rev diagnostics @ validationDiagnostics

                    if List.isEmpty allDiagnostics then
                        merged, []
                    else
                        defaultBindings, allDiagnostics
            with ex ->
                defaultBindings, [ Domain.diagnostic Error ("Hotkey config could not be loaded: " + ex.Message) (Some { Path = path; Line = None }) ]
