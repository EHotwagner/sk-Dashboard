namespace SkDashboard.Dashboard

open System
open System.Collections.Concurrent
open System.IO
open System.Threading
open Spectre.Console
open SkDashboard.Core

module Program =
    type CliOptions =
        { ProjectPath: string option
          RefreshInterval: TimeSpan option
          AutoCheckout: bool
          ConfigPath: string option
          SettingsMode: bool
          Keys: string list }

    type InteractiveEvent =
        | KeyPressed of string
        | SnapshotLoaded of DashboardSnapshot
        | ConfigChanged
        | RefreshFailed of Diagnostic

    let applyPreferences (preferences: DashboardPreferences) (snapshot: DashboardSnapshot) =
        { snapshot with
            Ui = preferences.Ui
            Diagnostics = snapshot.Diagnostics @ preferences.Diagnostics }

    let applyUi (preferences: DashboardPreferences) (snapshot: DashboardSnapshot) =
        { snapshot with Ui = preferences.Ui }

    let preferencesFromSnapshot bindings diagnostics (snapshot: DashboardSnapshot) =
        { Bindings = bindings
          Ui = snapshot.Ui
          Diagnostics = diagnostics }

    let saveSettingsSnapshot configPath activePreferences snapshot =
        let updated =
            preferencesFromSnapshot activePreferences.Bindings activePreferences.Diagnostics snapshot

        Hotkeys.writePreferences configPath updated
        Hotkeys.loadPreferences configPath

    let themeEditCommand command =
        command = SettingsAppThemePrevious
        || command = SettingsAppThemeNext
        || command = SettingsMarkdownThemePrevious
        || command = SettingsMarkdownThemeNext

    let applyCommandWithPreferences projectPath configPath activePreferences command state =
        let next = App.applyCommand projectPath command state

        if command = Refresh then
            applyUi activePreferences next
        elif App.settingsSurfaceActive state && themeEditCommand command then
            let ui, diagnostics = Hotkeys.resolveUiThemes configPath next.Ui

            { next with
                Ui = ui
                Diagnostics = next.Diagnostics @ diagnostics }
        else
            next

    let settingsEditCommand command =
        command = TableScrollLeft
        || command = TableScrollRight
        || command = TaskPrevious
        || command = TaskNext
        || command = DetailScrollLeft
        || command = DetailScrollRight
        || themeEditCommand command

    let saveConflictDiagnostic () =
        Domain.diagnostic
            Warning
            "Settings save conflict: config changed on disk. Reload or overwrite before saving."
            None

    let deferredReloadDiagnostic () =
        Domain.diagnostic Warning "External config change deferred while settings has unsaved edits." None

    let commandRequiresRender command =
        match command with
        | FeaturePrevious
        | FeatureNext
        | StoryPrevious
        | StoryNext
        | TaskPrevious
        | TaskNext
        | TableScrollLeft
        | TableScrollRight
        | DetailScrollUp
        | DetailScrollDown
        | DetailScrollLeft
        | DetailScrollRight
        | DetailsOpen
        | DetailsClose
        | FullScreenFeature
        | FullScreenStory
        | FullScreenPlan
        | FullScreenTask
        | ConstitutionOpen
        | ChecklistOpen
        | SettingsOpen
        | SettingsSave
        | SettingsDiscard
        | SettingsReload
        | SettingsOverwrite
        | SettingsAppThemePrevious
        | SettingsAppThemeNext
        | SettingsMarkdownThemePrevious
        | SettingsMarkdownThemeNext
        | HotkeysReload
        | Refresh -> true
        | FeatureCheckout
        | PaneNext
        | PanePrevious
        | Quit -> false

    let runScriptedKeys projectPath (configPath: string) preferences options =
        let snapshot =
            match options.RefreshInterval with
            | None ->
                App.loadWithAutoCheckout options.AutoCheckout projectPath
                |> applyPreferences preferences
            | Some interval ->
                use refreshed = new ManualResetEventSlim(false)

                let mutable latest =
                    App.loadWithAutoCheckout options.AutoCheckout projectPath
                    |> applyPreferences preferences

                use handle =
                    App.startRefreshOrchestration
                        projectPath
                        interval
                        (fun snapshot ->
                            latest <- snapshot |> applyPreferences (Hotkeys.loadPreferences configPath)
                            refreshed.Set())
                        (fun diagnostic ->
                            latest <-
                                { latest with
                                    Diagnostics = latest.Diagnostics @ [ diagnostic ] }

                            refreshed.Set())

                refreshed.Wait(interval + TimeSpan.FromMilliseconds 500.0) |> ignore
                latest

        let mutable activePreferences = preferences
        let mutable settingsLoadedVersion: ConfigFileVersion option = None
        let mutable settingsDirty = false

        let finalSnapshot =
            options.Keys
            |> List.fold
                (fun (state: DashboardSnapshot) key ->
                    match Input.commandForKeyWithBindings activePreferences.Bindings key with
                    | None -> state
                    | Some HotkeysReload
                    | Some SettingsReload
                    | Some SettingsDiscard ->
                        activePreferences <- Hotkeys.loadPreferences configPath
                        settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                        settingsDirty <- false
                        applyPreferences activePreferences state
                    | Some SettingsSave when
                        settingsDirty
                        && settingsLoadedVersion.IsSome
                        && settingsLoadedVersion.Value <> Hotkeys.currentConfigVersion configPath
                        ->
                        { state with
                            Diagnostics = state.Diagnostics @ [ saveConflictDiagnostic () ] }
                    | Some SettingsSave
                    | Some SettingsOverwrite ->
                        let updated =
                            preferencesFromSnapshot activePreferences.Bindings activePreferences.Diagnostics state

                        Hotkeys.writePreferences configPath updated
                        activePreferences <- Hotkeys.loadPreferences configPath
                        settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                        settingsDirty <- false
                        applyPreferences activePreferences state
                    | Some Quit -> state
                    | Some command ->
                        let next = applyCommandWithPreferences projectPath configPath activePreferences command state

                        if command = SettingsOpen then
                            settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                            settingsDirty <- false
                        elif App.settingsSurfaceActive state && settingsEditCommand command then
                            activePreferences <- saveSettingsSnapshot configPath activePreferences next
                            settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                            settingsDirty <- false

                        next)
                snapshot

        Render.renderSnapshot finalSnapshot

    let runInteractive projectPath (configPath: string) preferences options =
        let refreshInterval =
            options.RefreshInterval |> Option.defaultValue (TimeSpan.FromSeconds 2.0)

        let mutable snapshot =
            App.loadWithAutoCheckout options.AutoCheckout projectPath
            |> applyPreferences preferences

        let mutable activePreferences = preferences
        let mutable settingsLoadedVersion: ConfigFileVersion option = None
        let mutable settingsDirty = false
        let mutable running = true
        use events = new BlockingCollection<InteractiveEvent>()

        let enqueue event =
            if not events.IsAddingCompleted then
                events.Add event

        let inputThread =
            Thread(
                ThreadStart(fun () ->
                    while running do
                        Input.readKeySequence () |> KeyPressed |> enqueue)
            )

        inputThread.IsBackground <- true

        use refreshHandle =
            if activePreferences.Ui.LiveReload.Enabled then
                App.startRefreshOrchestration
                    projectPath
                    (TimeSpan.FromMilliseconds(float activePreferences.Ui.LiveReload.DebounceMilliseconds)
                     + refreshInterval)
                    (SnapshotLoaded >> enqueue)
                    (RefreshFailed >> enqueue)
            else
                { new IDisposable with
                    member _.Dispose() = () }

        let configDirectory =
            Path.GetDirectoryName configPath
            |> Option.ofObj
            |> Option.filter (String.IsNullOrWhiteSpace >> not)
            |> Option.defaultValue "."

        let configName =
            Path.GetFileName configPath |> Option.ofObj |> Option.defaultValue "*"

        use configWatcher =
            if activePreferences.Ui.LiveReload.Enabled && Directory.Exists configDirectory then
                let watcher = new FileSystemWatcher(configDirectory)
                watcher.Filter <- configName
                watcher.EnableRaisingEvents <- true
                watcher.Changed.Add(fun _ -> enqueue ConfigChanged)
                watcher.Created.Add(fun _ -> enqueue ConfigChanged)
                watcher.Renamed.Add(fun _ -> enqueue ConfigChanged)
                watcher.Deleted.Add(fun _ -> enqueue ConfigChanged)
                watcher :> IDisposable
            else
                { new IDisposable with
                    member _.Dispose() = () }

        let applyEvent event =
            match event with
            | SnapshotLoaded next ->
                let reloaded = Hotkeys.loadPreferences configPath
                activePreferences <- reloaded
                snapshot <- App.preserveSelections snapshot next |> applyPreferences activePreferences
            | ConfigChanged ->
                if App.settingsSurfaceActive snapshot && settingsDirty then
                    snapshot <-
                        { snapshot with
                            Diagnostics = snapshot.Diagnostics @ [ deferredReloadDiagnostic () ] }
                else
                    let reloaded = Hotkeys.loadPreferences configPath
                    activePreferences <- reloaded
                    settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                    snapshot <- applyPreferences activePreferences snapshot
            | RefreshFailed diagnostic ->
                snapshot <-
                    { snapshot with
                        Diagnostics = snapshot.Diagnostics @ [ diagnostic ] }
            | KeyPressed key ->
                match Input.commandForKeyWithBindings activePreferences.Bindings key with
                | None -> ()
                | Some Quit -> running <- false
                | Some HotkeysReload
                | Some SettingsReload
                | Some SettingsDiscard ->
                    activePreferences <- Hotkeys.loadPreferences configPath
                    settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                    settingsDirty <- false
                    snapshot <- applyPreferences activePreferences snapshot
                | Some SettingsSave when
                    settingsDirty
                    && settingsLoadedVersion.IsSome
                    && settingsLoadedVersion.Value <> Hotkeys.currentConfigVersion configPath
                    ->
                    snapshot <-
                        { snapshot with
                            Diagnostics = snapshot.Diagnostics @ [ saveConflictDiagnostic () ] }
                | Some SettingsSave
                | Some SettingsOverwrite ->
                    let updated =
                        preferencesFromSnapshot activePreferences.Bindings activePreferences.Diagnostics snapshot

                    Hotkeys.writePreferences configPath updated
                    activePreferences <- Hotkeys.loadPreferences configPath
                    settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                    settingsDirty <- false
                    snapshot <- applyPreferences activePreferences snapshot
                | Some command ->
                    let previous = snapshot
                    snapshot <- applyCommandWithPreferences projectPath configPath activePreferences command snapshot

                    if command = SettingsOpen then
                        settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                        settingsDirty <- false
                    elif App.settingsSurfaceActive previous && settingsEditCommand command then
                        activePreferences <- saveSettingsSnapshot configPath activePreferences snapshot
                        settingsLoadedVersion <- Some(Hotkeys.currentConfigVersion configPath)
                        settingsDirty <- false

        try
            Console.CursorVisible <- false
            inputThread.Start()

            AnsiConsole
                .Live(Render.snapshotRenderable snapshot)
                .AutoClear(false)
                .Start(fun context ->
                    context.Refresh()

                    while running do
                        let mutable event = Unchecked.defaultof<InteractiveEvent>

                        if events.TryTake(&event, 100) then
                            let previousText = Render.snapshotText snapshot

                            let forceRender =
                                match event with
                                | KeyPressed key ->
                                    Input.commandForKeyWithBindings activePreferences.Bindings key
                                    |> Option.exists commandRequiresRender
                                | SnapshotLoaded _
                                | ConfigChanged
                                | RefreshFailed _ -> true

                            applyEvent event

                            if running && (forceRender || Render.snapshotText snapshot <> previousText) then
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
        | "--settings" :: rest -> parseArgs rest { options with SettingsMode = true }
        | "--keys" :: value :: rest ->
            let keys =
                value.Split(',', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
                |> Array.toList

            parseArgs
                rest
                { options with
                    Keys = options.Keys @ keys }
        | "--refresh-interval" :: value :: rest ->
            match Int32.TryParse value with
            | true, ms when ms > 0 ->
                parseArgs
                    rest
                    { options with
                        RefreshInterval = Some(TimeSpan.FromMilliseconds(float ms)) }
            | _ -> invalidArg "--refresh-interval" "Refresh interval must be a positive integer in milliseconds."
        | value :: rest when value.StartsWith "--" -> invalidArg value "Unsupported option."
        | value :: rest ->
            parseArgs
                rest
                { options with
                    ProjectPath = Some value }

    let settingsText (configPath: string) (preferences: DashboardPreferences) =
        let diagnostics =
            match preferences.Diagnostics with
            | [] -> "Diagnostics: none"
            | values -> values |> List.map (fun d -> "Diagnostics: " + d.Message) |> String.concat "\n"

        sprintf
            "sk-dashboard settings\nConfig: %s\nApp theme: %s\nMarkdown theme: %s\nResolved Markdown theme: %s\nApp theme choices: %s\nMarkdown theme choices: %s\nTable border: %s\nSticky columns: %d\nTable horizontal step: %d\nDetail wrap: %b\nDetail horizontal step: %d\nLive reload: %b\nLive reload debounce: %dms\n%s"
            configPath
            preferences.Ui.Themes.SelectedAppThemeId
            preferences.Ui.Themes.SelectedMarkdownThemeId
            preferences.Ui.Markdown.Id
            (String.concat ", " preferences.Ui.Themes.AvailableAppThemes)
            (String.concat ", " preferences.Ui.Themes.AvailableMarkdownThemes)
            (Domain.tableBorderId preferences.Ui.Table.Border)
            preferences.Ui.Table.StickyColumns
            preferences.Ui.Table.HorizontalStep
            preferences.Ui.Detail.WrapText
            preferences.Ui.Detail.HorizontalStep
            preferences.Ui.LiveReload.Enabled
            preferences.Ui.LiveReload.DebounceMilliseconds
            diagnostics

    let runSettingsMode (configPath: string) (preferences: DashboardPreferences) =
        let before = Hotkeys.currentConfigVersion configPath
        let session = Domain.settingsSession preferences.Ui before

        if not (File.Exists configPath) then
            Hotkeys.writePreferences configPath preferences

        let after = Hotkeys.currentConfigVersion configPath

        let status =
            if session.LoadedVersion <> before then "conflict"
            elif after.LastWriteTimeUtc.IsSome then "ready"
            else "default"

        Console.WriteLine(settingsText configPath preferences)
        Console.WriteLine("Settings status: " + status)

    [<EntryPoint>]
    let main argv =
        let options =
            parseArgs
                (Array.toList argv)
                { ProjectPath = None
                  RefreshInterval = None
                  AutoCheckout = true
                  ConfigPath = None
                  SettingsMode = false
                  Keys = [] }

        let projectPath = options.ProjectPath |> Option.defaultValue "."

        let configPath =
            options.ConfigPath
            |> Option.defaultValue (SpeckitArtifacts.resolveUserConfigPath ())

        let preferences = Hotkeys.loadPreferences configPath

        if options.SettingsMode then
            runSettingsMode configPath preferences
        elif List.isEmpty options.Keys && not Console.IsInputRedirected then
            runInteractive projectPath configPath preferences options
        else
            runScriptedKeys projectPath configPath preferences options

        0
