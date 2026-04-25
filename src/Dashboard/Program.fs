namespace SkDashboard.Dashboard

open System
open System.Collections.Concurrent
open System.Threading
open Spectre.Console
open SkDashboard.Core

module Program =
    type CliOptions =
        { ProjectPath: string option
          RefreshInterval: TimeSpan option
          AutoCheckout: bool
          ConfigPath: string option
          Keys: string list }

    type InteractiveEvent =
        | KeyPressed of string
        | SnapshotLoaded of DashboardSnapshot
        | RefreshFailed of Diagnostic

    let applyPreferences (preferences: DashboardPreferences) (snapshot: DashboardSnapshot) =
        { snapshot with
            Ui = preferences.Ui
            Diagnostics = snapshot.Diagnostics @ preferences.Diagnostics }

    let applyUi (preferences: DashboardPreferences) (snapshot: DashboardSnapshot) =
        { snapshot with Ui = preferences.Ui }

    let runScriptedKeys projectPath configPath preferences options =
        let snapshot =
            match options.RefreshInterval with
            | None -> App.loadWithAutoCheckout options.AutoCheckout projectPath |> applyPreferences preferences
            | Some interval ->
                use refreshed = new ManualResetEventSlim(false)
                let mutable latest = App.loadWithAutoCheckout options.AutoCheckout projectPath |> applyPreferences preferences

                use handle =
                    App.startRefreshOrchestration
                        projectPath
                        interval
                        (fun snapshot ->
                            latest <- snapshot |> applyPreferences (Hotkeys.loadPreferences configPath)
                            refreshed.Set())
                        (fun diagnostic ->
                            latest <- { latest with Diagnostics = latest.Diagnostics @ [ diagnostic ] }
                            refreshed.Set())

                refreshed.Wait(interval + TimeSpan.FromMilliseconds 500.0) |> ignore
                latest

        let mutable activePreferences = preferences

        let finalSnapshot =
            options.Keys
            |> List.fold
                (fun (state: DashboardSnapshot) key ->
                    match Input.commandForKeyWithBindings activePreferences.Bindings key with
                    | None -> state
                    | Some HotkeysReload ->
                        activePreferences <- Hotkeys.loadPreferences configPath
                        applyPreferences activePreferences state
                    | Some Quit -> state
                    | Some command -> App.applyCommand projectPath command state |> applyUi activePreferences)
                snapshot

        Render.renderSnapshot finalSnapshot

    let runInteractive projectPath configPath preferences options =
        let refreshInterval = options.RefreshInterval |> Option.defaultValue (TimeSpan.FromSeconds 2.0)
        let mutable snapshot =
            App.loadWithAutoCheckout options.AutoCheckout projectPath
            |> applyPreferences preferences

        let mutable activePreferences = preferences
        let mutable running = true
        use events = new BlockingCollection<InteractiveEvent>()

        let enqueue event =
            if not events.IsAddingCompleted then
                events.Add event

        let inputThread =
            Thread(ThreadStart(fun () ->
                while running do
                    Input.readKeySequence () |> KeyPressed |> enqueue))

        inputThread.IsBackground <- true

        use refreshHandle =
            App.startRefreshOrchestration
                projectPath
                refreshInterval
                (SnapshotLoaded >> enqueue)
                (RefreshFailed >> enqueue)

        let applyEvent event =
            match event with
            | SnapshotLoaded next ->
                let reloaded = Hotkeys.loadPreferences configPath
                activePreferences <- reloaded
                snapshot <- App.preserveSelections snapshot next |> applyPreferences activePreferences
            | RefreshFailed diagnostic -> snapshot <- { snapshot with Diagnostics = snapshot.Diagnostics @ [ diagnostic ] }
            | KeyPressed key ->
                match Input.commandForKeyWithBindings activePreferences.Bindings key with
                | None -> ()
                | Some Quit -> running <- false
                | Some HotkeysReload ->
                    activePreferences <- Hotkeys.loadPreferences configPath
                    snapshot <- applyPreferences activePreferences snapshot
                | Some command -> snapshot <- App.applyCommand projectPath command snapshot |> applyUi activePreferences

        try
            Console.CursorVisible <- false
            inputThread.Start()

            AnsiConsole
                .Live(Render.snapshotRenderable snapshot)
                .AutoClear(false)
                .Start(fun context ->
                    while running do
                        let mutable event = Unchecked.defaultof<InteractiveEvent>

                        if events.TryTake(&event, 100) then
                            let previousText = Render.snapshotText snapshot
                            applyEvent event

                            if running && Render.snapshotText snapshot <> previousText then
                                context.UpdateTarget(Render.snapshotRenderable snapshot)
                                context.Refresh())
        finally
            events.CompleteAdding()
            Console.CursorVisible <- true

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
        let preferences = Hotkeys.loadPreferences configPath

        if List.isEmpty options.Keys && not Console.IsInputRedirected then
            runInteractive projectPath configPath preferences options
        else
            runScriptedKeys projectPath configPath preferences options

        0
