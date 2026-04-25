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
    | Refresh
    | HotkeysReload
    | Quit

type HotkeyBinding =
    { Command: DashboardCommand
      KeySequence: string
      Scope: string
      Source: string }

module Hotkeys =
    val defaultBindings : HotkeyBinding list
    val validateBindings : HotkeyBinding list -> Diagnostic list
    val commandId : DashboardCommand -> string
    val loadBindings : string -> HotkeyBinding list * Diagnostic list
