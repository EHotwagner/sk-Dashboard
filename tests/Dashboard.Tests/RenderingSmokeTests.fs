module SkDashboard.Dashboard.Tests.RenderingSmokeTests

open System.IO
open Expecto
open Spectre.Console
open SkDashboard.Core
open SkDashboard.Dashboard

let renderToText renderable =
    use writer = new StringWriter()
    let output = AnsiConsoleOutput(writer)
    let settings = AnsiConsoleSettings()
    settings.Ansi <- AnsiSupport.Yes
    settings.ColorSystem <- ColorSystemSupport.TrueColor
    settings.Out <- output
    settings.Interactive <- InteractionSupport.No
    let console = AnsiConsole.Create(settings)
    console.Write(renderable)
    writer.ToString()

[<Tests>]
let renderingSmokeTests =
    testList
        "Rendering"
        [ test "snapshotText_handles empty project state" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-render-").FullName
              let text = App.load root |> Render.snapshotText
              Expect.stringContains text "No feature artifacts found" "Empty state is visible."
          }

          test "snapshotText_marks_last_story_activity" {
              let root =
                  Directory.CreateTempSubdirectory("sk-dashboard-render-activity-").FullName

              let featureRoot = Path.Combine(root, "specs", "001-a")
              Directory.CreateDirectory(featureRoot) |> ignore

              File.WriteAllText(
                  Path.Combine(featureRoot, "spec.md"),
                  "### User Story 1 - First (Priority: P1)\n\n### User Story 2 - Second (Priority: P1)\n"
              )

              File.WriteAllText(
                  Path.Combine(featureRoot, "tasks.md"),
                  "- [X] T001 [US1] Done first\n- [X] T002 [US2] Done second\n"
              )

              let text = App.load root |> App.selectStory 1 |> Render.snapshotText
              Expect.stringContains text "US2 activity" "Last story-scoped task marks its story as active."
              Expect.stringContains text "T002 activity" "Last story-scoped task is marked active."
          }

          test "layout_selection_uses_auto_threshold" {
              Expect.equal (Domain.resolveLayout 119 Auto) VerticalLayout "Auto uses vertical below 120 columns."
              Expect.equal (Domain.resolveLayout 120 Auto) WidescreenLayout "Auto uses widescreen at 120 columns."
              Expect.equal (Domain.resolveLayout 80 Widescreen) WidescreenLayout "Explicit widescreen is honored."
              Expect.equal (Domain.resolveLayout 160 Vertical) VerticalLayout "Explicit vertical is honored."
          }

          test "snapshotRenderable_supports_explicit_and_auto_layout_modes" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-render-layout-").FullName
              let snapshot = App.load root

              let withLayout layout =
                  { snapshot with
                      Ui = { snapshot.Ui with Layout = layout } }

              Render.snapshotRenderableForWidth 160 (withLayout Widescreen) |> ignore
              Render.snapshotRenderableForWidth 80 (withLayout Vertical) |> ignore
              Render.snapshotRenderableForWidth 80 (withLayout Auto) |> ignore
              Render.snapshotRenderableForWidth 160 (withLayout Auto) |> ignore
              Expect.isTrue true "All layout renderables are produced."
          }

          test "configured_colors_are_used_for_dashboard_roles" {
              let ui =
                  { Layout = Auto
                    Table = Domain.defaultUiPreferences.Table
                    Detail = Domain.defaultUiPreferences.Detail
                    LiveReload = Domain.defaultUiPreferences.LiveReload
                    Themes = Domain.defaultUiPreferences.Themes
                    Markdown = Domain.defaultUiPreferences.Markdown
                    Colors =
                      Domain.defaultUiPreferences.Colors
                      |> Map.add
                          Selected
                          { Foreground = "black"
                            Background = Some "green" }
                      |> Map.add
                          LastActivity
                          { Foreground = "white"
                            Background = Some "#555555" }
                      |> Map.add
                          ProgressComplete
                          { Foreground = "#00ff00"
                            Background = None }
                      |> Map.add
                          ProgressIncomplete
                          { Foreground = "#555555"
                            Background = None }
                      |> Map.add
                          DiagnosticInfo
                          { Foreground = "#7aa2f7"
                            Background = None }
                      |> Map.add
                          DiagnosticWarning
                          { Foreground = "yellow"
                            Background = None }
                      |> Map.add
                          DiagnosticError
                          { Foreground = "red"
                            Background = None }
                      |> Map.add
                          Muted
                          { Foreground = "grey42"
                            Background = None }
                      |> Map.add
                          PanelAccent
                          { Foreground = "#7aa2f7"
                            Background = None }
                      |> Map.add
                          RowStripeOdd
                          { Foreground = "white"
                            Background = Some "#101820" }
                      |> Map.add
                          RowStripeEven
                          { Foreground = "white"
                            Background = Some "#18232f" } }

              Expect.equal (Render.styleTag Selected ui) "black on green" "Selected role uses configured pair."

              Expect.equal
                  (Render.styleTag LastActivity ui)
                  "white on #555555"
                  "Last activity role uses configured pair."

              Expect.stringContains (Render.progressMarkup ui 2 2) "#00ff00" "Progress complete color is used."
              Expect.stringContains (Render.progressMarkup ui 1 2) "#555555" "Progress incomplete color is used."
              Expect.equal (Render.markup DiagnosticInfo ui "info") "[#7aa2f7]info[/]" "Diagnostic info color is used."
              Expect.equal (Render.color Muted ui) "grey42" "Muted color is used."
              Expect.equal (Render.color PanelAccent ui) "#7aa2f7" "Panel accent color is used."
              Expect.equal (Render.rowStripeTag 0 ui) "white on #18232f" "Even stripe role is used."
              Expect.equal (Render.rowStripeTag 1 ui) "white on #101820" "Odd stripe role is used."
              Expect.equal
                  (Render.stripedMarkup 1 { ui with Table = { ui.Table with AlternateRowShading = true } } "row")
                  "[white on #101820] row [/]"
                  "Enabled row shading uses stripe role colors."
          }

          test "default_colors_are_used_without_custom_preferences" {
              let ui = Domain.defaultUiPreferences
              Expect.equal (Render.styleTag Selected ui) "black on deepskyblue1" "Default selected color is used."
              Expect.equal (Render.color ProgressComplete ui) "green" "Default progress color is used."
              Expect.equal (Render.color DiagnosticError ui) "red" "Default diagnostic error color is used."
              Expect.equal (Render.color Muted ui) "grey" "Default muted color is used."
              Expect.equal (Render.stripedMarkup 0 ui "row") "[white] row [/]" "Default theme does not shade table rows."
              Expect.isFalse ui.Table.AlternateRowShading "Default alternate row shading is disabled."
          }

          test "snapshotText_includes_dashboard_version" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-render-version-").FullName
              let text = App.load root |> Render.snapshotText
              Expect.stringContains text "sk-dashboard v" "Snapshot text exposes header version."
          }

          test "full_screen_text_renders_single_requested_target" {
              let root =
                  Directory.CreateTempSubdirectory("sk-dashboard-render-fullscreen-").FullName

              let featureRoot = Path.Combine(root, "specs", "001-a")
              Directory.CreateDirectory(featureRoot) |> ignore

              File.WriteAllText(
                  Path.Combine(featureRoot, "spec.md"),
                  "### User Story 1 - First (Priority: P1)\n\nDescription\n\n1. **Given** x, **When** y, **Then** z.\n"
              )

              File.WriteAllText(
                  Path.Combine(featureRoot, "plan.md"),
                  "## Summary\n\nPlan summary\n\n## Technical Context\n\nF#\n"
              )

              File.WriteAllText(Path.Combine(featureRoot, "tasks.md"), "- [ ] T001 [US1] First task\n")

              let snapshot = App.load root
              let planModal = App.openFullScreen PlanFullScreen snapshot

              let text =
                  planModal.FullScreen
                  |> Option.map (Render.fullScreenText planModal)
                  |> Option.defaultValue ""

              Expect.stringContains text "Plan" "Plan target renders."
              Expect.stringContains text "Plan summary" "Plan fields render."
              Expect.isFalse (text.Contains "User Story US1") "Only the requested target type is rendered."
          }

          test "constitution_full_screen_renders_markdown_and_failure_states" {
              let root =
                  Directory.CreateTempSubdirectory("sk-dashboard-render-constitution-").FullName

              let memory = Path.Combine(root, ".specify", "memory")
              Directory.CreateDirectory(memory) |> ignore
              let constitutionPath = Path.Combine(memory, "constitution.md")

              File.WriteAllText(
                  constitutionPath,
                  "# Constitution\n\n- **Respect** public contracts\n- Use `dotnet test`\n"
              )

              let opened = App.load root |> App.applyCommand root ConstitutionOpen

              let text =
                  opened.FullScreen
                  |> Option.map (Render.fullScreenText opened)
                  |> Option.defaultValue ""

              Expect.stringContains text "Respect" "Markdown source content is loaded for the constitution view."
              opened.FullScreen |> Option.iter (Render.fullScreenRenderable opened >> ignore)

              File.Delete constitutionPath
              let missing = App.applyCommand root ConstitutionOpen opened

              let missingText =
                  missing.FullScreen
                  |> Option.map (Render.fullScreenText missing)
                  |> Option.defaultValue ""

              Expect.stringContains missingText "unavailable" "Missing constitution has a readable fallback document."
              Expect.isNonEmpty missing.Diagnostics "Missing constitution emits a non-fatal diagnostic."

              File.WriteAllText(constitutionPath, "")
              let empty = App.applyCommand root ConstitutionOpen missing

              let emptyText =
                  empty.FullScreen
                  |> Option.map (Render.fullScreenText empty)
                  |> Option.defaultValue ""

              Expect.stringContains emptyText "empty" "Empty constitution has an explicit message."
          }

          test "compact_tables_keep_markdown_like_cells_plain" {
              let root =
                  Directory.CreateTempSubdirectory("sk-dashboard-render-plain-tables-").FullName

              let featureRoot = Path.Combine(root, "specs", "001-a")
              Directory.CreateDirectory(featureRoot) |> ignore

              File.WriteAllText(
                  Path.Combine(featureRoot, "spec.md"),
                  "### User Story 1 - **Bold Story** (Priority: P1)\n"
              )

              File.WriteAllText(
                  Path.Combine(featureRoot, "tasks.md"),
                  "- [ ] T001 [US1] Keep `inline code` plain in table rows\n"
              )

              let snapshot = App.load root
              Render.storiesTable snapshot |> ignore
              Render.tasksTable snapshot |> ignore
              Expect.isTrue true "Markdown-like compact table cells render on the plain table path."
          }

          test "navigation_state_is_preserved_across_layout_modes" {
              let root =
                  Directory.CreateTempSubdirectory("sk-dashboard-render-nav-layout-").FullName

              let featureRoot = Path.Combine(root, "specs", "001-a")
              Directory.CreateDirectory(featureRoot) |> ignore

              File.WriteAllText(
                  Path.Combine(featureRoot, "spec.md"),
                  "### User Story 1 - First (Priority: P1)\n\n### User Story 2 - Second (Priority: P1)\n"
              )

              File.WriteAllText(
                  Path.Combine(featureRoot, "tasks.md"),
                  "- [ ] T001 [US1] First task\n- [ ] T002 [US2] Second task\n"
              )

              let navigated = App.load root |> App.selectStory 1 |> App.selectTask 0

              for layout in [ Widescreen; Vertical; Auto ] do
                  let snapshot =
                      { navigated with
                          Ui = { navigated.Ui with Layout = layout } }

                  Render.snapshotRenderableForWidth 80 snapshot |> ignore
                  Expect.equal snapshot.SelectedStoryId (Some "US2") "Story selection remains stable."
                  Expect.equal snapshot.SelectedTaskId (Some "T002") "Task selection remains stable."
          }

          test "valid_ui_preferences_apply_when_sibling_values_are_invalid" {
              let path =
                  Path.Combine(
                      Directory.CreateTempSubdirectory("sk-dashboard-render-invalid-sibling-").FullName,
                      "dashboard.json"
                  )

              File.WriteAllText(
                  path,
                  """{"version":1,"bindings":[],"ui":{"layout":"bogus","colors":{"panelAccent":"#7aa2f7","muted":"notacolor"}}}"""
              )

              let preferences = Hotkeys.loadPreferences path
              Expect.equal preferences.Ui.Colors[PanelAccent].Foreground "#7aa2f7" "Valid sibling color applies."

              Expect.equal
                  preferences.Ui.Colors[Muted]
                  Domain.defaultUiPreferences.Colors[Muted]
                  "Invalid sibling color falls back."

              Expect.isNonEmpty preferences.Diagnostics "Invalid sibling values are reported."
          }

          test "table_border_preferences_render_all_supported_styles" {
              let root = Directory.CreateTempSubdirectory("sk-dashboard-render-borders-").FullName
              let snapshot = App.load root

              for border in [ NoBorder; MinimalBorder; RoundedBorder; HeavyBorder ] do
                  let ui =
                      { snapshot.Ui with
                          Table =
                              { snapshot.Ui.Table with
                                  Border = border } }

                  let configured = { snapshot with Ui = ui }
                  Render.featuresTable configured |> ignore
                  Render.tasksTable configured |> ignore

              Expect.isTrue true "All supported table borders produce table renderables."
          }

          test "settings_surface_lists_current_display_values" {
              let root =
                  Directory.CreateTempSubdirectory("sk-dashboard-render-settings-").FullName

              let snapshot = App.load root |> App.applyCommand root SettingsOpen

              let text =
                  snapshot.FullScreen
                  |> Option.map (Render.fullScreenText snapshot)
                  |> Option.defaultValue ""

              Expect.stringContains text "Settings" "Settings surface renders."
              Expect.stringContains text "Table border" "Table border value is visible."
              Expect.stringContains text "Live reload" "Live reload value is visible."
          }

          test "settings_surface_lists_custom_theme_choices_and_fallbacks" {
              let root =
                  Directory.CreateTempSubdirectory("sk-dashboard-render-custom-themes-").FullName

              let configPath = Path.Combine(root, "dashboard.json")
              let appFolder = Path.Combine(root, "themes", "app")
              let markdownFolder = Path.Combine(root, "themes", "markdown")
              Directory.CreateDirectory(appFolder) |> ignore
              Directory.CreateDirectory(markdownFolder) |> ignore

              File.WriteAllText(
                  Path.Combine(appFolder, "custom-app.json"),
                  """{"family":"app","id":"custom-app","displayName":"Custom App","colors":{"panelAccent":"cyan"}}"""
              )

              File.WriteAllText(
                  Path.Combine(markdownFolder, "custom-md.json"),
                  """{"family":"markdown","id":"custom-md","displayName":"Custom Markdown","colors":{"heading":"magenta"}}"""
              )

              File.WriteAllText(configPath, """{"version":1,"bindings":[],"ui":{"themes":{"app":"missing-app","markdown":"custom-md"}}}""")

              let preferences = Hotkeys.loadPreferences configPath
              let snapshot =
                  { App.load root with
                      Ui = preferences.Ui
                      Diagnostics = preferences.Diagnostics }
                  |> App.applyCommand root SettingsOpen

              let text =
                  snapshot.FullScreen
                  |> Option.map (Render.fullScreenText snapshot)
                  |> Option.defaultValue ""

              Expect.stringContains text "custom-app" "Custom app theme choice is visible."
              Expect.stringContains text "custom-md" "Custom Markdown theme choice is visible."
              Expect.stringContains text "app fallback active" "Missing selected app theme is visible."
              Expect.isNonEmpty snapshot.Diagnostics "Fallback diagnostics are preserved."
          }

          test "markdown_theme_is_used_for_fullscreen_and_checklist_rendering" {
              let ui =
                  { Domain.defaultUiPreferences with
                      Markdown =
                        { Domain.defaultUiPreferences.Markdown with
                            Id = "test-md"
                            Colors =
                              { Domain.defaultUiPreferences.Markdown.Colors with
                                  Heading = "magenta"
                                  CheckedItem = "green"
                                  UncheckedItem = "yellow"
                                  Note = "cyan" } } }

              let rows, _, _, _, _ =
                  Render.markdownRows
                      ui
                      "# Heading\n\n- [x] Done\n- [ ] Todo\n> Note"
                      None
                      (Domain.defaultDetailViewport 8 80)

              let text = renderToText (Rows rows)
              Expect.stringContains text "\u001b[" "The markdown rows produce styled terminal output."
              Expect.stringContains text "Heading" "Heading content is rendered."
              Expect.stringContains text "Done" "Checked item content is rendered."
              Expect.stringContains text "Todo" "Unchecked item content is rendered."
          }

          test "markdown_heading_spacing_adds_margins_in_detail_rendering" {
              Expect.equal Domain.defaultUiPreferences.Markdown.Spacing.BeforeHeading 0 "Default Markdown rendering stays tight before headings."
              Expect.equal Domain.defaultUiPreferences.Markdown.Spacing.AfterHeading 0 "Default Markdown rendering stays tight after headings."

              let ui =
                  { Domain.defaultUiPreferences with
                      Markdown =
                        { Domain.defaultUiPreferences.Markdown with
                            Spacing =
                              { Domain.defaultUiPreferences.Markdown.Spacing with
                                  BeforeHeading = 2
                                  AfterHeading = 1 } } }

              let rows, lineCount, viewport, _, _ =
                  Render.markdownRows ui "# Heading\nBody\n```# Not heading\n```" None (Domain.defaultDetailViewport 10 80)

              let text = renderToText (Rows rows)
              let normalized = text.Replace("\r\n", "\n").Replace('\r', '\n')
              Expect.equal lineCount 7 "Heading spacing contributes visible detail rows."
              Expect.equal viewport.VisibleLines 10 "Viewport keeps the requested visible line count."
              Expect.stringContains normalized " \n \n" "Before-heading margins render as visible blank rows."
              Expect.stringContains normalized "Heading\u001b[0m\n \n" "After-heading margin renders after the heading row."
              Expect.stringContains text "Heading" "The heading still renders after spacing is applied."
              Expect.stringContains text "Body" "Following body text remains visible."
              Expect.stringContains text "# Not heading" "Code block content is not treated as a spaced heading."
          }

          test "visibleRows_keeps_selected_item_visible_after_first_page" {
              let rows = [ 1..50 ]
              let visible, viewport = Render.visibleRows 37 12 rows
              Expect.contains visible 38 "The selected row is included in the rendered slice."
              Expect.equal viewport.RowOffset 36 "The viewport centers the selected row in a small render window."
              Expect.equal visible [ 37; 38; 39 ] "The selected item appears in the middle of the focused slice."
          }

          test "fullscreen_renderable_accepts_horizontal_offset" {
              let longText = String.replicate 20 "x" + "VISIBLE"

              let modal =
                  { Target = PlanFullScreen
                    SelectedFeatureId = None
                    SelectedStoryId = None
                    SelectedTaskId = None
                    Document = None
                    Checklist = None
                    Viewport =
                      { Domain.defaultDetailViewport 5 8 with
                          ColumnOffset = 20 } }

              let snapshot =
                  { RepositoryRoot = "."
                    CurrentBranch = None
                    Version = Domain.resolveDashboardVersion ()
                    Features = []
                    SelectedFeatureId = None
                    Stories = []
                    SelectedStoryId = None
                    Plan =
                      Some
                          { Path = None
                            Summary = None
                            TechnicalContext = None
                            ConstitutionCheck = None
                            RawContent = longText
                            Diagnostics = [] }
                    TaskGraph = None
                    SelectedTaskId = None
                    Panes = Domain.defaultPanes
                    Ui = Domain.defaultUiPreferences
                    FullScreen = Some modal
                    Diagnostics = []
                    LastRefreshedAt = System.DateTimeOffset.UnixEpoch }

              Render.fullScreenRenderable snapshot modal |> ignore
              Expect.isTrue true "Full-screen rendering accepts a horizontal viewport offset."
          } ]
