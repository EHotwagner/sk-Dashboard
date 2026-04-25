namespace SkDashboard.Core

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
    val defaultBindings : HotkeyBinding list
    val validateBindings : HotkeyBinding list -> Diagnostic list
    val commandId : DashboardCommand -> string
    val colorRoleDefaults : unit -> Map<DashboardColorRole, DashboardColorStyle>
    val parseLayoutMode : string -> DashboardLayoutMode option
    val loadPreferences : string -> DashboardPreferences
    val loadBindings : string -> HotkeyBinding list * Diagnostic list
