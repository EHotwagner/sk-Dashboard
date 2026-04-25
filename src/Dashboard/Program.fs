namespace SkDashboard.Dashboard

open System
open System.Threading
open SkDashboard.Core

module Program =
    type CliOptions =
        { ProjectPath: string option
          RefreshInterval: TimeSpan option
          AutoCheckout: bool
          ConfigPath: string option
          Keys: string list }

    let rec parseArgs args options =
        match args with
        | [] -> options
        | "--no-auto-checkout" :: rest -> parseArgs rest { options with AutoCheckout = false }
        | "--config" :: value :: rest -> parseArgs rest { options with ConfigPath = Some value }
        | "--keys" :: value :: rest ->
            let keys =
                value.Split(',', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
                |> Array.toList

            parseArgs rest { options with Keys = options.Keys @ keys }
        | "--refresh-interval" :: value :: rest ->
            match Int32.TryParse value with
            | true, ms when ms > 0 -> parseArgs rest { options with RefreshInterval = Some(TimeSpan.FromMilliseconds(float ms)) }
            | _ -> invalidArg "--refresh-interval" "Refresh interval must be a positive integer in milliseconds."
        | value :: rest when value.StartsWith "--" -> invalidArg value "Unsupported option."
        | value :: rest -> parseArgs rest { options with ProjectPath = Some value }

    [<EntryPoint>]
    let main argv =
        let options = parseArgs (Array.toList argv) { ProjectPath = None; RefreshInterval = None; AutoCheckout = true; ConfigPath = None; Keys = [] }
        let projectPath = options.ProjectPath |> Option.defaultValue "."
        let configPath = options.ConfigPath |> Option.defaultValue (SpeckitArtifacts.resolveUserConfigPath ())
        let bindings, hotkeyDiagnostics = Hotkeys.loadBindings configPath

        let snapshot =
            match options.RefreshInterval with
            | None -> App.loadWithAutoCheckout options.AutoCheckout projectPath
            | Some interval ->
                use refreshed = new ManualResetEventSlim(false)
                let mutable latest = App.loadWithAutoCheckout options.AutoCheckout projectPath

                use handle =
                    App.startRefreshOrchestration
                        projectPath
                        interval
                        (fun snapshot ->
                            latest <- snapshot
                            refreshed.Set())
                        (fun diagnostic ->
                            latest <- { latest with Diagnostics = latest.Diagnostics @ [ diagnostic ] }
                            refreshed.Set())

                refreshed.Wait(interval + TimeSpan.FromMilliseconds 500.0) |> ignore
                latest

        let snapshot =
            { snapshot with Diagnostics = snapshot.Diagnostics @ hotkeyDiagnostics }

        let mutable activeBindings = bindings

        let finalSnapshot =
            options.Keys
            |> List.fold
                (fun (state: DashboardSnapshot) key ->
                    match Input.commandForKeyWithBindings activeBindings key with
                    | None -> state
                    | Some HotkeysReload ->
                        let reloaded, diagnostics = Hotkeys.loadBindings configPath
                        activeBindings <- reloaded
                        { state with Diagnostics = state.Diagnostics @ diagnostics }
                    | Some command -> App.applyCommand projectPath command state)
                snapshot

        Render.renderSnapshot finalSnapshot
        0
