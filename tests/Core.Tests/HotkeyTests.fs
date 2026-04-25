module SkDashboard.Core.Tests.HotkeyTests

open System.IO
open Expecto
open SkDashboard.Core

[<Tests>]
let hotkeyTests =
    testList
        "Hotkeys"
        [ test "defaultBindings_cover primary commands" {
              let commands = Hotkeys.defaultBindings |> List.map _.Command |> Set.ofList

              Expect.equal commands.Count 29 "Every primary command has a default binding."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = FeaturePrevious))
                      .KeySequence
                  "k"
                  "Feature previous defaults to k."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = FeatureNext))
                      .KeySequence
                  "j"
                  "Feature next defaults to j."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = StoryPrevious))
                      .KeySequence
                  "up"
                  "Story previous defaults to up arrow."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = StoryNext))
                      .KeySequence
                  "down"
                  "Story next defaults to down arrow."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = TaskPrevious))
                      .KeySequence
                  "left"
                  "Task previous defaults to left arrow."

              Expect.equal
                  (Hotkeys.defaultBindings |> List.find (fun binding -> binding.Command = TaskNext)).KeySequence
                  "right"
                  "Task next defaults to right arrow."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = FullScreenFeature))
                      .KeySequence
                  "F"
                  "Feature full-screen defaults to F."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = FullScreenStory))
                      .KeySequence
                  "S"
                  "Story full-screen defaults to S."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = FullScreenPlan))
                      .KeySequence
                  "P"
                  "Plan full-screen defaults to P."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = FullScreenTask))
                      .KeySequence
                  "T"
                  "Task full-screen defaults to T."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = SettingsOpen))
                      .KeySequence
                  ","
                  "Settings opens by hotkey."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = TableScrollLeft))
                      .KeySequence
                  "h"
                  "Table horizontal scroll left is reachable."

              Expect.equal
                  (Hotkeys.defaultBindings
                   |> List.find (fun binding -> binding.Command = DetailScrollDown))
                      .KeySequence
                  "v"
                  "Detail scroll down is reachable."
          }

          test "validateBindings_reports duplicate keys" {
              let bindings =
                  [ { Command = Refresh
                      KeySequence = "r"
                      Scope = "dashboard"
                      Source = "test" }
                    { Command = Quit
                      KeySequence = "r"
                      Scope = "dashboard"
                      Source = "test" } ]

              Expect.isNonEmpty (Hotkeys.validateBindings bindings) "Duplicate key is a conflict."
          }

          test "loadBindings_applies valid global override" {
              let path =
                  Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-hotkeys-").FullName, "hotkeys.json")

              File.WriteAllText(path, """{"version":1,"bindings":[{"command":"story.next","key":"n"}]}""")
              let bindings, diagnostics = Hotkeys.loadBindings path
              Expect.isEmpty diagnostics "Valid config has no diagnostics."
              let storyNext = bindings |> List.find (fun binding -> binding.Command = StoryNext)
              Expect.equal storyNext.KeySequence "n" "User override is active."
          }

          test "loadBindings_reports unsupported_unknown_and_conflicting_bindings" {
              let path =
                  Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-hotkeys-bad-").FullName, "hotkeys.json")

              File.WriteAllText(
                  path,
                  """{"version":1,"bindings":[{"command":"story.next","key":"bad key"},{"command":"refresh","key":"q"},{"command":"quit","key":"q"},{"command":"unknown","key":"u"}]}"""
              )

              let bindings, diagnostics = Hotkeys.loadBindings path
              Expect.equal bindings Hotkeys.defaultBindings "Invalid config falls back to defaults."
              let messages = diagnostics |> List.map _.Message |> String.concat "\n"
              Expect.stringContains messages "Unsupported hotkey sequence" "Unsupported keys are reported."
              Expect.stringContains messages "Conflicting hotkey binding" "Conflicts are reported."
              Expect.stringContains messages "Unknown hotkey command" "Unknown commands are reported."
          }

          test "loadPreferences_parses combined bindings_and_ui" {
              let path =
                  Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-prefs-").FullName, "dashboard.json")

              File.WriteAllText(
                  path,
                  """{"version":1,"bindings":[{"command":"story.next","key":"n"},{"command":"fullscreen.task","key":"x"}],"ui":{"layout":"vertical","table":{"border":"heavy","stickyColumns":2,"horizontalStep":12},"detail":{"wrapText":true,"horizontalStep":16},"liveReload":{"enabled":true,"debounceMilliseconds":150},"colors":{"selected":{"foreground":"black","background":"green"},"panelAccent":"#7aa2f7","rowStripeOdd":{"foreground":"white","background":"#101820"},"detailHeading":"cyan"}}}"""
              )

              let preferences = Hotkeys.loadPreferences path
              Expect.isEmpty preferences.Diagnostics "Valid combined preferences have no diagnostics."

              Expect.equal
                  (preferences.Bindings |> List.find (fun binding -> binding.Command = StoryNext)).KeySequence
                  "n"
                  "Hotkey override is active."

              Expect.equal
                  (preferences.Bindings
                   |> List.find (fun binding -> binding.Command = FullScreenTask))
                      .KeySequence
                  "x"
                  "Full-screen hotkey override is active."

              Expect.equal preferences.Ui.Layout Vertical "Layout is parsed."
              Expect.equal preferences.Ui.Table.Border HeavyBorder "Table border is parsed."
              Expect.equal preferences.Ui.Table.StickyColumns 2 "Sticky columns are parsed."
              Expect.equal preferences.Ui.Table.HorizontalStep 12 "Table horizontal step is parsed."
              Expect.isTrue preferences.Ui.Detail.WrapText "Detail wrap is parsed."
              Expect.equal preferences.Ui.Detail.HorizontalStep 16 "Detail horizontal step is parsed."
              Expect.equal preferences.Ui.LiveReload.DebounceMilliseconds 150 "Live reload debounce is parsed."

              Expect.equal
                  preferences.Ui.Colors[Selected]
                  { Foreground = "black"
                    Background = Some "green" }
                  "Selected pair is parsed."

              Expect.equal
                  preferences.Ui.Colors[PanelAccent]
                  { Foreground = "#7aa2f7"
                    Background = None }
                  "Hex color is parsed."

              Expect.equal
                  preferences.Ui.Colors[RowStripeOdd]
                  { Foreground = "white"
                    Background = Some "#101820" }
                  "Stripe role is parsed."

              Expect.equal
                  preferences.Ui.Colors[DetailHeading]
                  { Foreground = "cyan"
                    Background = None }
                  "Detail role is parsed."
          }

          test "viewport_reducers_clamp_scroll_and_selection" {
              let viewport =
                  { Domain.defaultTableViewport 5 10 with
                      RowOffset = 99
                      ColumnOffset = 99
                      StickyColumns = 20 }

              let clamped = Domain.clampTableViewport 20 40 viewport
              Expect.equal clamped.RowOffset 15 "Row offset is clamped to the last full page."
              Expect.equal clamped.ColumnOffset 30 "Column offset is clamped to the last visible column."
              Expect.equal clamped.StickyColumns 10 "Sticky columns cannot exceed visible columns."

              let selected = Domain.defaultTableViewport 5 10 |> Domain.keepRowVisible 12 20
              Expect.equal selected.RowOffset 8 "Selection anchored scrolling keeps the selected row visible."

              let detail =
                  { Domain.defaultDetailViewport 10 20 with
                      LineOffset = 500
                      ColumnOffset = 500 }

              let detailClamped = Domain.clampDetailViewport 100 80 detail
              Expect.equal detailClamped.LineOffset 90 "Detail line offset is clamped."
              Expect.equal detailClamped.ColumnOffset 60 "Detail column offset is clamped."
          }

          test "settings_sessions_and_live_reload_track_conflicts" {
              let version =
                  { Path = "dashboard.json"
                    LastWriteTimeUtc = None
                    Length = None }

              let nextVersion = { version with Length = Some 42L }
              let session = Domain.settingsSession Domain.defaultUiPreferences version

              let draft =
                  { Domain.defaultUiPreferences with
                      Table =
                          { Domain.defaultUiPreferences.Table with
                              Border = HeavyBorder } }

              let dirty = Domain.updateSettingsDraft draft session
              Expect.isTrue dirty.Dirty "Changing the draft marks the session dirty."

              let conflicted = Domain.markSettingsConflict nextVersion dirty
              Expect.equal conflicted.Conflict (StaleSettingsSave nextVersion) "Stale saves are represented."

              let live =
                  Domain.liveReloadState Domain.defaultUiPreferences
                  |> Domain.deferLiveReload nextVersion

              Expect.equal live.PendingVersion (Some nextVersion) "Dirty sessions can defer external reloads."
          }

          test "loadPreferences_defaults_ui_when_absent" {
              let path =
                  Path.Combine(
                      Directory.CreateTempSubdirectory("sk-dashboard-prefs-default-").FullName,
                      "dashboard.json"
                  )

              File.WriteAllText(path, """{"version":1,"bindings":[{"command":"story.next","key":"n"}]}""")

              let preferences = Hotkeys.loadPreferences path
              Expect.isEmpty preferences.Diagnostics "Missing UI is valid."
              Expect.equal preferences.Ui Domain.defaultUiPreferences "Default UI preferences are used."
          }

          test "loadPreferences_accepts_named_and_hex_colors" {
              let path =
                  Path.Combine(
                      Directory.CreateTempSubdirectory("sk-dashboard-prefs-colors-").FullName,
                      "dashboard.json"
                  )

              File.WriteAllText(
                  path,
                  """{"version":1,"bindings":[],"ui":{"colors":{"progressComplete":"green","diagnosticInfo":"#00bfff"}}}"""
              )

              let preferences = Hotkeys.loadPreferences path
              Expect.isEmpty preferences.Diagnostics "Named and hex colors are valid."
              Expect.equal preferences.Ui.Colors[ProgressComplete].Foreground "green" "Named color is retained."
              Expect.equal preferences.Ui.Colors[DiagnosticInfo].Foreground "#00bfff" "Hex color is retained."
          }

          test "loadPreferences_reports_invalid_ui_values_with_partial_fallback" {
              let path =
                  Path.Combine(
                      Directory.CreateTempSubdirectory("sk-dashboard-prefs-invalid-").FullName,
                      "dashboard.json"
                  )

              File.WriteAllText(
                  path,
                  """{"version":1,"bindings":[{"command":"story.next","key":"n"}],"ui":{"layout":"cinema","colors":{"progressComplete":"green","muted":"notacolor","extraRole":"red"}}}"""
              )

              let preferences = Hotkeys.loadPreferences path
              let messages = preferences.Diagnostics |> List.map _.Message |> String.concat "\n"

              Expect.equal
                  (preferences.Bindings |> List.find (fun binding -> binding.Command = StoryNext)).KeySequence
                  "n"
                  "Valid hotkeys still apply."

              Expect.equal preferences.Ui.Layout Auto "Invalid layout falls back."
              Expect.equal preferences.Ui.Colors[ProgressComplete].Foreground "green" "Valid sibling colors apply."

              Expect.equal
                  preferences.Ui.Colors[Muted]
                  Domain.defaultUiPreferences.Colors[Muted]
                  "Invalid color role falls back."

              Expect.stringContains messages "Unsupported layout mode" "Unsupported layout is reported."
              Expect.stringContains messages "Invalid color" "Invalid color is reported."
              Expect.stringContains messages "Unknown color role" "Unknown roles are reported."
          }

          test "loadPreferences_reports_low_contrast_pairs_and_uses_defaults" {
              let path =
                  Path.Combine(
                      Directory.CreateTempSubdirectory("sk-dashboard-prefs-contrast-").FullName,
                      "dashboard.json"
                  )

              File.WriteAllText(
                  path,
                  """{"version":1,"bindings":[],"ui":{"colors":{"selected":{"foreground":"white","background":"#eeeeee"},"panelAccent":"red"}}}"""
              )

              let preferences = Hotkeys.loadPreferences path

              Expect.equal
                  preferences.Ui.Colors[Selected]
                  Domain.defaultUiPreferences.Colors[Selected]
                  "Low-contrast pair falls back."

              Expect.equal preferences.Ui.Colors[PanelAccent].Foreground "red" "Valid sibling value still applies."

              Expect.exists
                  preferences.Diagnostics
                  (fun diagnostic -> diagnostic.Message.Contains "Low-contrast")
                  "Low contrast is reported."
          }

          test "loadPreferences_recovers_from_empty_and_malformed_files" {
              let emptyPath =
                  Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-prefs-empty-").FullName, "dashboard.json")

              let malformedPath =
                  Path.Combine(
                      Directory.CreateTempSubdirectory("sk-dashboard-prefs-malformed-").FullName,
                      "dashboard.json"
                  )

              File.WriteAllText(emptyPath, "")
              File.WriteAllText(malformedPath, """{"version":1,"bindings":[""")

              let empty = Hotkeys.loadPreferences emptyPath
              let malformed = Hotkeys.loadPreferences malformedPath

              Expect.equal empty.Bindings Hotkeys.defaultBindings "Empty file uses default bindings."
              Expect.equal empty.Ui Domain.defaultUiPreferences "Empty file uses default UI."
              Expect.isNonEmpty empty.Diagnostics "Empty file reports a diagnostic."
              Expect.equal malformed.Bindings Hotkeys.defaultBindings "Malformed file uses default bindings."
              Expect.equal malformed.Ui Domain.defaultUiPreferences "Malformed file uses default UI."
              Expect.isNonEmpty malformed.Diagnostics "Malformed file reports a diagnostic."
          }

          test "public_preference_surface_exposes_ui_contract_types" {
              let preferences: DashboardPreferences =
                  { Bindings = Hotkeys.defaultBindings
                    Ui = Domain.defaultUiPreferences
                    Diagnostics = [] }

              let roles =
                  [ Selected
                    LastActivity
                    ProgressComplete
                    ProgressIncomplete
                    DiagnosticInfo
                    DiagnosticWarning
                    DiagnosticError
                    Muted
                    PanelAccent
                    RowStripeOdd
                    RowStripeEven
                    DetailHeading
                    DetailLabel
                    DetailBody
                    DetailSource ]

              Expect.equal (Hotkeys.parseLayoutMode "auto") (Some Auto) "Auto layout is exposed."
              Expect.equal (Hotkeys.parseLayoutMode "widescreen") (Some Widescreen) "Widescreen layout is exposed."
              Expect.equal (Hotkeys.parseLayoutMode "vertical") (Some Vertical) "Vertical layout is exposed."

              Expect.equal
                  (Domain.resolveLayout 119 preferences.Ui.Layout)
                  VerticalLayout
                  "Resolved layout type is exposed."

              Expect.equal roles.Length 15 "All required color roles are exposed."

              Expect.isTrue
                  (roles |> List.forall (fun role -> preferences.Ui.Colors.ContainsKey role))
                  "Default colors cover every exposed role."
          }

          test "resolveDashboardVersion_returns_readable_label" {
              let version = Domain.resolveDashboardVersion ()
              Expect.stringStarts version.Label "v" "Version label has a visible prefix."
              Expect.isNonEmpty version.Label "Version label is never empty."
          }

          test "normalizeDashboardVersionLabel_removes_build_metadata" {
              Expect.equal
                  (Domain.normalizeDashboardVersionLabel "0.1.7+48219e0a025968403d78e68def0a9a186bb62d41")
                  (Some "v0.1.7")
                  "Commit metadata is hidden from the dashboard header."

              Expect.equal
                  (Domain.normalizeDashboardVersionLabel "v0.1.7-beta.1+48219e0")
                  (Some "v0.1.7-beta.1")
                  "Prerelease labels are preserved while build metadata is hidden."
          } ]
