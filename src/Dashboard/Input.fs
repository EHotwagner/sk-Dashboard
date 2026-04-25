namespace SkDashboard.Dashboard

open SkDashboard.Core

type DashboardEvent =
    | RefreshRequested
    | QuitRequested
    | NoOp

module Input =
    let commandForKeyWithBindings bindings key =
        bindings
        |> List.tryFind (fun binding -> binding.KeySequence = key)
        |> Option.map _.Command

    let commandForKey key =
        commandForKeyWithBindings Hotkeys.defaultBindings key

    let eventForCommand command =
        match command with
        | Quit -> QuitRequested
        | Refresh -> RefreshRequested
        | _ -> NoOp
