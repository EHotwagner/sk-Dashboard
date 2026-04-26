module SkDashboard.Dashboard.Tests.StateReducerTests

open System
open System.IO
open System.Threading
open Expecto
open SkDashboard.Core
open SkDashboard.Dashboard

[<Tests>]
let stateReducerTests =
    testList
        "Input"
        [ test "commandForKey_maps refresh and quit" {
              Expect.isSome (Input.commandForKey "r") "Refresh is reachable."
              Expect.isSome (Input.commandForKey "q") "Quit is reachable."
          }

          test "keySequenceFromConsoleKey_normalizes interactive keys" {
              Expect.equal
                  (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false)))
                  "q"
                  "Printable keys are preserved."

              Expect.equal
                  (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('\000', ConsoleKey.Enter, false, false, false)))
                  "enter"
                  "Enter is named."

              Expect.equal
                  (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('\t', ConsoleKey.Tab, true, false, false)))
                  "shift+tab"
                  "Shift-tab is named."

              Expect.equal
                  (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('\000', ConsoleKey.DownArrow, false, false, false)))
                  "down"
                  "Down arrow is named."

              Expect.equal
                  (Input.keySequenceFromConsoleKey (ConsoleKeyInfo('\000', ConsoleKey.RightArrow, true, false, false)))
                  "shift+right"
                  "Shift-right is named."
          }

          test "commandForKey_maps feature and story navigation" {
              Expect.equal (Input.commandForKey "k") (Some FeaturePrevious) "Feature previous is reachable."
              Expect.equal (Input.commandForKey "j") (Some FeatureNext) "Feature next is reachable."
              Expect.equal (Input.commandForKey "up") (Some StoryPrevious) "Story previous is reachable."
              Expect.equal (Input.commandForKey "down") (Some StoryNext) "Story next is reachable."
              Expect.equal (Input.commandForKey "left") (Some TaskPrevious) "Task previous is reachable."
              Expect.equal (Input.commandForKey "right") (Some TaskNext) "Task next is reachable."
              Expect.equal (Input.commandForKey "F") (Some FullScreenFeature) "Feature full-screen is reachable."
              Expect.equal (Input.commandForKey "S") (Some FullScreenStory) "Story full-screen is reachable."
              Expect.equal (Input.commandForKey "P") (Some FullScreenPlan) "Plan full-screen is reachable."
              Expect.equal (Input.commandForKey "T") (Some FullScreenTask) "Task full-screen is reachable."
              Expect.equal (Input.commandForKey "C") (Some ConstitutionOpen) "Constitution view is reachable."
              Expect.equal (Input.commandForKey "L") (Some ChecklistOpen) "Checklist view is reachable."
              Expect.equal (Input.commandForKey ",") (Some SettingsOpen) "Settings page is reachable."
              Expect.equal (Input.commandForKey "A") (Some SettingsAppThemeNext) "App theme next is reachable."
              Expect.equal (Input.commandForKey "M") (Some SettingsMarkdownThemeNext) "Markdown theme next is reachable."
              Expect.equal (Input.commandForKey "h") (Some TableScrollLeft) "Table scroll left is reachable."
              Expect.equal (Input.commandForKey "l") (Some TableScrollRight) "Table scroll right is reachable."
              Expect.equal (Input.commandForKey "u") (Some DetailScrollUp) "Detail scroll up is reachable."
              Expect.equal (Input.commandForKey "v") (Some DetailScrollDown) "Detail scroll down is reachable."
          }

          test "commandForKeyWithBindings_applies valid override" {
              let bindings =
                  Hotkeys.defaultBindings
                  |> List.map (fun binding ->
                      if binding.Command = StoryNext then
                          { binding with
                              KeySequence = "n"
                              Source = "test" }
                      else
                          binding)

              Expect.equal (Input.commandForKeyWithBindings bindings "n") (Some StoryNext) "Custom story key is routed."
          }

          test "selectFeature_updates selected feature state" {
              let status = Domain.emptyFeatureStatus DateTimeOffset.UnixEpoch

              let feature id =
                  { Id = id
                    BranchName = Some id
                    DisplayName = id
                    OrderKey = Fallback id
                    IsSelected = id = "001-a"
                    ArtifactRoot = None
                    CheckoutState = NotAttempted
                    Status = Some status }

              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = [ feature "001-a"; feature "002-b" ]
                    SelectedFeatureId = Some "001-a"
                    Stories = []
                    SelectedStoryId = None
                    Plan = None
                    TaskGraph = None
                    SelectedTaskId = None
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let next = App.selectFeature 1 snapshot
              Expect.equal next.SelectedFeatureId (Some "002-b") "Next feature is selected."
              Expect.equal (next.Features |> List.map _.IsSelected) [ false; true ] "Feature selection flags update."
          }

          test "selectStory_updates selected story state" {
              let story id =
                  { Id = id
                    Title = id
                    Priority = None
                    Description = ""
                    AcceptanceScenarios = []
                    SourceLocation = None }

              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = None
                    Stories = [ story "US1"; story "US2" ]
                    SelectedStoryId = Some "US1"
                    Plan = None
                    TaskGraph = None
                    SelectedTaskId = None
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let next = App.selectStory 1 snapshot
              Expect.equal next.SelectedStoryId (Some "US2") "Next story is selected."
          }

          test "applyCommand_routes feature and story navigation" {
              let story id =
                  { Id = id
                    Title = id
                    Priority = None
                    Description = ""
                    AcceptanceScenarios = []
                    SourceLocation = None }

              let feature id =
                  { Id = id
                    BranchName = Some id
                    DisplayName = id
                    OrderKey = Fallback id
                    IsSelected = id = "001-a"
                    ArtifactRoot = None
                    CheckoutState = NotAttempted
                    Status = None }

              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = [ feature "001-a"; feature "002-b" ]
                    SelectedFeatureId = Some "001-a"
                    Stories = [ story "US1"; story "US2" ]
                    SelectedStoryId = Some "US1"
                    Plan = None
                    TaskGraph = None
                    SelectedTaskId = None
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let next =
                  snapshot |> App.applyCommand "." FeatureNext |> App.applyCommand "." StoryNext

              Expect.equal next.SelectedFeatureId (Some "002-b") "FeatureNext is applied."
              Expect.equal next.SelectedStoryId (Some "US2") "StoryNext is applied."
          }

          test "selectTask_updates selected task detail state" {
              let task id =
                  { Id = id
                    Title = id
                    Description = None
                    RawStatus = "[ ]"
                    Dependencies = []
                    RelatedStoryId = Some "US1"
                    SourceLocation = None
                    Metadata = Map.empty }

              let graph =
                  { SelectedStoryId = Some "US1"
                    Nodes = [ task "T001"; task "T002" ]
                    Edges = []
                    Diagnostics = []
                    SelectedTaskId = Some "T001" }

              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = None
                    Stories = []
                    SelectedStoryId = Some "US1"
                    Plan = None
                    TaskGraph = Some graph
                    SelectedTaskId = Some "T001"
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let next = App.selectTask 1 snapshot
              Expect.equal next.SelectedTaskId (Some "T002") "Next task is selected."
              Expect.equal (next.TaskGraph |> Option.bind _.SelectedTaskId) (Some "T002") "Graph selected task follows."
          }

          test "selectStory_rebuilds_task_graph_for_selected_story" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-story-").FullName
              let featureRoot = Path.Combine(root, "specs", "001-a")
              Directory.CreateDirectory(featureRoot) |> ignore

              File.WriteAllText(
                  Path.Combine(featureRoot, "tasks.md"),
                  "- [ ] T001 [US1] First story task\n- [ ] T002 [US2] Second story task\n"
              )

              let feature =
                  { Id = "001-a"
                    BranchName = Some "001-a"
                    DisplayName = "001-a"
                    OrderKey = Fallback "001-a"
                    IsSelected = true
                    ArtifactRoot = Some featureRoot
                    CheckoutState = NotAttempted
                    Status = None }

              let story id =
                  { Id = id
                    Title = id
                    Priority = None
                    Description = ""
                    AcceptanceScenarios = []
                    SourceLocation = None }

              let graph =
                  { SelectedStoryId = Some "US1"
                    Nodes =
                      [ { Id = "T001"
                          Title = "[US1] First story task"
                          Description = None
                          RawStatus = "[ ]"
                          Dependencies = []
                          RelatedStoryId = Some "US1"
                          SourceLocation = None
                          Metadata = Map.empty } ]
                    Edges = []
                    Diagnostics = []
                    SelectedTaskId = Some "T001" }

              let snapshot =
                  { RepositoryRoot = root
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = [ feature ]
                    SelectedFeatureId = Some "001-a"
                    Stories = [ story "US1"; story "US2" ]
                    SelectedStoryId = Some "US1"
                    Plan = None
                    TaskGraph = Some graph
                    SelectedTaskId = Some "T001"
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let next = App.selectStory 1 snapshot
              Expect.equal next.SelectedStoryId (Some "US2") "Story selection moved."

              Expect.equal
                  (next.TaskGraph |> Option.map _.Nodes |> Option.defaultValue [] |> List.map _.Id)
                  [ "T002" ]
                  "Task graph was rebuilt for the new story."

              Expect.equal next.SelectedTaskId (Some "T002") "Task selection follows rebuilt graph."
          }

          test "full_screen_commands_open_replace_and_close_without_selection_changes" {
              let task id =
                  { Id = id
                    Title = id
                    Description = None
                    RawStatus = "[ ]"
                    Dependencies = []
                    RelatedStoryId = Some "US1"
                    SourceLocation = None
                    Metadata = Map.empty }

              let graph =
                  { SelectedStoryId = Some "US1"
                    Nodes = [ task "T001"; task "T002" ]
                    Edges = []
                    Diagnostics = []
                    SelectedTaskId = Some "T002" }

              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = Some "001-a"
                    Stories = []
                    SelectedStoryId = Some "US1"
                    Plan = None
                    TaskGraph = Some graph
                    SelectedTaskId = Some "T002"
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let featureModal = App.applyCommand "." FullScreenFeature snapshot

              Expect.equal
                  (featureModal.FullScreen |> Option.map _.Target)
                  (Some FeatureFullScreen)
                  "Feature modal opens."

              Expect.equal featureModal.SelectedFeatureId snapshot.SelectedFeatureId "Feature selection is preserved."
              Expect.equal featureModal.SelectedStoryId snapshot.SelectedStoryId "Story selection is preserved."
              Expect.equal featureModal.SelectedTaskId snapshot.SelectedTaskId "Task selection is preserved."

              let taskModal = App.applyCommand "." FullScreenTask featureModal

              Expect.equal
                  (taskModal.FullScreen |> Option.map _.Target)
                  (Some TaskFullScreen)
                  "A second modal command replaces the target."

              let closed = App.applyCommand "." DetailsClose taskModal
              Expect.isNone closed.FullScreen "Close clears only modal state."
              Expect.equal closed.SelectedFeatureId snapshot.SelectedFeatureId "Feature selection remains after close."
              Expect.equal closed.SelectedStoryId snapshot.SelectedStoryId "Story selection remains after close."
              Expect.equal closed.SelectedTaskId snapshot.SelectedTaskId "Task selection remains after close."
          }

          test "constitution_open_reads_current_file_and_close_preserves_context" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-constitution-").FullName
              let memory = Path.Combine(root, ".specify", "memory")
              Directory.CreateDirectory(memory) |> ignore
              let constitutionPath = Path.Combine(memory, "constitution.md")
              File.WriteAllText(constitutionPath, "# Constitution\n\n- Current rule")

              let snapshot =
                  { RepositoryRoot = root
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = Some "001-a"
                    Stories = []
                    SelectedStoryId = Some "US1"
                    Plan = None
                    TaskGraph = None
                    SelectedTaskId = Some "T001"
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let opened = App.applyCommand root ConstitutionOpen snapshot

              Expect.equal
                  (opened.FullScreen |> Option.map _.Target)
                  (Some ConstitutionFullScreen)
                  "Constitution view opens."

              Expect.stringContains
                  (opened.FullScreen
                   |> Option.bind _.Document
                   |> Option.map _.RawContent
                   |> Option.defaultValue "")
                  "Current rule"
                  "Constitution file content is loaded."

              File.WriteAllText(constitutionPath, "# Constitution\n\n- Changed rule")
              let reopened = App.applyCommand root ConstitutionOpen opened

              Expect.stringContains
                  (reopened.FullScreen
                   |> Option.bind _.Document
                   |> Option.map _.RawContent
                   |> Option.defaultValue "")
                  "Changed rule"
                  "The file is re-read on every open command."

              let closed = App.applyCommand root DetailsClose reopened
              Expect.isNone closed.FullScreen "Close clears the constitution view."
              Expect.equal closed.SelectedFeatureId snapshot.SelectedFeatureId "Feature context is preserved."
              Expect.equal closed.SelectedStoryId snapshot.SelectedStoryId "Story context is preserved."
              Expect.equal closed.SelectedTaskId snapshot.SelectedTaskId "Task context is preserved."
          }

          test "checklist_hotkey_opens_active_feature_checklist_and_preserves_context" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-checklist-hotkey-").FullName
              let featureRoot = Path.Combine(root, "specs", "001-a")
              Directory.CreateDirectory(Path.Combine(featureRoot, "checklists")) |> ignore

              File.WriteAllText(Path.Combine(featureRoot, "spec.md"), "### User Story 1 - First (Priority: P1)\n")
              File.WriteAllText(Path.Combine(featureRoot, "plan.md"), "## Summary\nPlan\n")
              File.WriteAllText(Path.Combine(featureRoot, "tasks.md"), "- [ ] T001 [US1] First task\n")
              File.WriteAllText(Path.Combine(featureRoot, "checklists", "requirements.md"), "# Requirements\n\n- [x] Done\n")

              let snapshot = App.load root |> App.selectStory 0
              let opened = App.applyCommand root ChecklistOpen snapshot

              Expect.equal (opened.FullScreen |> Option.map _.Target) (Some ChecklistFullScreen) "Checklist modal opens."
              Expect.equal opened.SelectedFeatureId snapshot.SelectedFeatureId "Feature selection is preserved."
              Expect.equal opened.SelectedStoryId snapshot.SelectedStoryId "Story selection is preserved."

              let reading = App.applyCommand root FeatureCheckout opened
              let text = reading.FullScreen |> Option.bind _.Document |> Option.map _.RawContent |> Option.defaultValue ""
              Expect.stringContains text "Requirements" "Enter opens the selected checklist document."
          }

          test "scroll_commands_update_table_and_full_detail_offsets" {
              let longPlanContent =
                  String.concat "\n" [ for i in 1..200 -> sprintf "line %d" i ]

              let plan =
                  { Path = Some "plan.md"
                    Summary = None
                    TechnicalContext = None
                    ConstitutionCheck = None
                    RawContent = longPlanContent
                    Diagnostics = [] }

              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = None
                    Stories = []
                    SelectedStoryId = None
                    Plan = Some plan
                    TaskGraph = None
                    SelectedTaskId = None
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let scrolled = App.applyCommand "." TableScrollRight snapshot

              let paneOffsets =
                  scrolled.Panes
                  |> List.filter (fun pane ->
                      pane.Kind = Features
                      || pane.Kind = Stories
                      || pane.Kind = TaskGraph
                      || pane.Kind = Diagnostics)
                  |> List.map _.ScrollOffset

              Expect.isTrue
                  (paneOffsets
                   |> List.forall ((=) Domain.defaultUiPreferences.Table.HorizontalStep))
                  "Horizontal table offset is stored on table panes."

              let full =
                  App.openFullScreen PlanFullScreen snapshot
                  |> App.applyCommand "." DetailScrollDown

              Expect.equal
                  (full.FullScreen |> Option.map _.Viewport.LineOffset)
                  (Some 5)
                  "Full detail scroll updates modal viewport."

              let bounded =
                  App.openFullScreen PlanFullScreen { snapshot with Plan = None }
                  |> App.applyCommand "." DetailScrollDown

              Expect.equal
                  (bounded.FullScreen |> Option.map _.Viewport.LineOffset)
                  (Some 0)
                  "Scrolling past the end of short content is clamped to the last visible line."
          }

          test "fullscreen_arrow_navigation_scrolls_detail_content" {
              let widePadding = String.replicate 200 "x"

              let longPlanContent =
                  String.concat "\n" [ for i in 1..200 -> sprintf "line %d %s" i widePadding ]

              let plan =
                  { Path = Some "plan.md"
                    Summary = None
                    TechnicalContext = None
                    ConstitutionCheck = None
                    RawContent = longPlanContent
                    Diagnostics = [] }

              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = None
                    Stories = []
                    SelectedStoryId = None
                    Plan = Some plan
                    TaskGraph = None
                    SelectedTaskId = None
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }
                  |> App.openFullScreen PlanFullScreen

              let down = App.applyCommand "." StoryNext snapshot

              Expect.equal
                  (down.FullScreen |> Option.map _.Viewport.LineOffset)
                  (Some 5)
                  "Down arrow scrolls full detail down."

              let right = App.applyCommand "." TaskNext down

              Expect.equal
                  (right.FullScreen |> Option.map _.Viewport.ColumnOffset)
                  (Some Domain.defaultUiPreferences.Detail.HorizontalStep)
                  "Right arrow scrolls full detail horizontally."

              let downAgain = App.applyCommand "." StoryNext right

              Expect.equal
                  (downAgain.FullScreen |> Option.map _.Viewport.ColumnOffset)
                  (Some Domain.defaultUiPreferences.Detail.HorizontalStep)
                  "Vertical scrolling preserves horizontal detail offset."
          }

          test "settings_command_opens_settings_surface_without_selection_changes" {
              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = Some "001-a"
                    Stories = []
                    SelectedStoryId = Some "US1"
                    Plan = None
                    TaskGraph = None
                    SelectedTaskId = Some "T001"
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let next = App.applyCommand "." SettingsOpen snapshot
              Expect.equal (next.FullScreen |> Option.map _.Target) (Some SettingsFullScreen) "Settings surface opens."
              Expect.equal next.SelectedFeatureId snapshot.SelectedFeatureId "Feature selection is preserved."
              Expect.equal next.SelectedStoryId snapshot.SelectedStoryId "Story selection is preserved."
              Expect.equal next.SelectedTaskId snapshot.SelectedTaskId "Task selection is preserved."

              Expect.exists
                  next.Diagnostics
                  (fun diagnostic -> diagnostic.Message.Contains "Settings page opened")
                  "Settings opening is visible in diagnostics."
          }

          test "settings_theme_commands_cycle_available_theme_ids" {
              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = None
                    Stories = []
                    SelectedStoryId = None
                    Plan = None
                    TaskGraph = None
                    SelectedTaskId = None
                    Panes = Domain.defaultPanes
                    Ui =
                      { Domain.defaultUiPreferences with
                          Themes =
                            { Domain.defaultUiPreferences.Themes with
                                AvailableAppThemes = [ "default"; "light"; "dark" ]
                                AvailableMarkdownThemes = [ "plain"; "default" ] } }
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }
                  |> App.applyCommand "." SettingsOpen

              let changedApp = App.applyCommand "." SettingsAppThemeNext snapshot
              Expect.equal changedApp.Ui.Themes.SelectedAppThemeId "light" "App theme advances in settings."
              Expect.equal changedApp.Ui.Colors Domain.defaultUiPreferences.Colors "App theme cycling clears previously resolved colors before reapplying."

              let changedMarkdown = App.applyCommand "." SettingsMarkdownThemePrevious snapshot
              Expect.equal changedMarkdown.Ui.Themes.SelectedMarkdownThemeId "plain" "Markdown theme cycles in settings."
          }

          test "settings_arrows_move_focus_and_change_focused_values" {
              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = None
                    Stories = []
                    SelectedStoryId = None
                    Plan = None
                    TaskGraph = None
                    SelectedTaskId = None
                    Panes = Domain.defaultPanes
                    Ui =
                      { Domain.defaultUiPreferences with
                          Themes =
                            { Domain.defaultUiPreferences.Themes with
                                AvailableAppThemes = [ "default"; "light"; "dark" ]
                                AvailableMarkdownThemes = [ "plain"; "default" ] } }
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }
                  |> App.applyCommand "." SettingsOpen

              let appChanged = App.applyCommand "." TaskNext snapshot
              Expect.equal appChanged.Ui.Themes.SelectedAppThemeId "light" "Right arrow changes focused app theme."

              let markdownFocused =
                  snapshot |> App.applyCommand "." StoryNext

              Expect.equal
                  (markdownFocused.FullScreen |> Option.map _.Viewport.LineOffset)
                  (Some 1)
                  "Down arrow moves settings focus."

              let markdownChanged = App.applyCommand "." TaskPrevious markdownFocused
              Expect.equal markdownChanged.Ui.Themes.SelectedMarkdownThemeId "plain" "Left arrow changes focused Markdown theme."
          }

          test "preserveSelections_keeps_visible_selection_across_refresh" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-preserve-").FullName

              let featureRoot id =
                  let path = Path.Combine(root, "specs", id)
                  Directory.CreateDirectory(path) |> ignore

                  File.WriteAllText(
                      Path.Combine(path, "tasks.md"),
                      "- [ ] T001 [US1] First task\n- [ ] T002 [US2] Second task\n"
                  )

                  path

              let feature id selected =
                  { Id = id
                    BranchName = Some id
                    DisplayName = id
                    OrderKey = Fallback id
                    IsSelected = selected
                    ArtifactRoot = Some(featureRoot id)
                    CheckoutState = NotAttempted
                    Status = None }

              let story id =
                  { Id = id
                    Title = id
                    Priority = None
                    Description = ""
                    AcceptanceScenarios = []
                    SourceLocation = None }

              let task id =
                  { Id = id
                    Title = id
                    Description = None
                    RawStatus = "[ ]"
                    Dependencies = []
                    RelatedStoryId = Some "US1"
                    SourceLocation = None
                    Metadata = Map.empty }

              let snapshot selectedFeature selectedStory selectedTask =
                  let graph =
                      { SelectedStoryId = Some "US1"
                        Nodes = [ task "T001"; task "T002" ]
                        Edges = []
                        Diagnostics = []
                        SelectedTaskId = selectedTask }

                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features =
                      [ feature "001-a" (selectedFeature = Some "001-a")
                        feature "002-b" (selectedFeature = Some "002-b") ]
                    SelectedFeatureId = selectedFeature
                    Stories = [ story "US1"; story "US2" ]
                    SelectedStoryId = selectedStory
                    Plan = None
                    TaskGraph = Some graph
                    SelectedTaskId = selectedTask
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = None
                    Diagnostics = []
                    LastRefreshedAt = DateTimeOffset.UnixEpoch }

              let previous = snapshot (Some "002-b") (Some "US2") (Some "T002")
              let refreshed = snapshot (Some "001-a") (Some "US1") (Some "T001")
              let next = App.preserveSelections previous refreshed

              Expect.equal next.SelectedFeatureId (Some "002-b") "Feature selection is preserved."
              Expect.equal next.SelectedStoryId (Some "US2") "Story selection is preserved."
              Expect.equal next.SelectedTaskId (Some "T002") "Task selection is preserved."

              Expect.equal
                  (next.Features |> List.map _.IsSelected)
                  [ false; true ]
                  "Feature selection flags follow preserved state."
          }

          test "refresh events coalesce into one pending snapshot load" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-refresh-").FullName

              let model =
                  App.initialRefreshModel root
                  |> App.enqueueRefresh Manual
                  |> App.enqueueRefresh FileChanged
                  |> App.enqueueRefresh FileChanged

              Expect.isTrue model.Pending "Refresh is pending."
              Expect.equal model.QueuedTriggers [ Manual; FileChanged ] "Duplicate refresh reasons are coalesced."

              let next, snapshot = App.drainRefresh model
              Expect.isFalse next.Pending "Refresh is drained."
              Expect.isSome snapshot "Refresh produces a new snapshot."
          }

          test "refresh orchestration observes polling and file changes" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-refresh-watch-").FullName
              use observed = new ManualResetEventSlim(false)
              let mutable count = 0

              use handle =
                  App.startRefreshOrchestration
                      root
                      (TimeSpan.FromMilliseconds 50.0)
                      (fun _ ->
                          count <- count + 1

                          if count >= 2 then
                              observed.Set())
                      (fun _ -> observed.Set())

              Directory.CreateDirectory(Path.Combine(root, "specs")) |> ignore
              File.WriteAllText(Path.Combine(root, "specs", "touch.txt"), "changed")

              Expect.isTrue (observed.Wait(TimeSpan.FromSeconds 3.0)) "Polling and file-system refreshes are observed."
              Expect.isGreaterThanOrEqual count 2 "At least two refresh callbacks were received."
          } ]
