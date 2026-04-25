namespace SkDashboard.Core

open System

type DiagnosticSeverity =
    | Info
    | Warning
    | Error

type SourceLocation =
    { Path: string
      Line: int option }

type Diagnostic =
    { Severity: DiagnosticSeverity
      Message: string
      Source: SourceLocation option }

type ArtifactState =
    | Missing
    | Present
    | Unreadable of string
    | Malformed of string
    | GraphInvalid of string

type CheckoutState =
    | NotAttempted
    | Current
    | CheckedOut
    | Failed of string

type FeatureOrderKey =
    | Numeric of int
    | Timestamp of string
    | Fallback of string

type FeatureStatus =
    { SpecState: ArtifactState
      PlanState: ArtifactState
      TasksState: ArtifactState
      ChecklistState: ArtifactState
      Diagnostics: Diagnostic list
      LastRefreshedAt: DateTimeOffset }

type Feature =
    { Id: string
      BranchName: string option
      DisplayName: string
      OrderKey: FeatureOrderKey
      IsSelected: bool
      ArtifactRoot: string option
      CheckoutState: CheckoutState
      Status: FeatureStatus option }

type UserStory =
    { Id: string
      Title: string
      Priority: string option
      Description: string
      AcceptanceScenarios: string list
      SourceLocation: SourceLocation option }

type Plan =
    { Path: string option
      Summary: string option
      TechnicalContext: string option
      ConstitutionCheck: string option
      RawContent: string
      Diagnostics: Diagnostic list }

type SpeckitTask =
    { Id: string
      Title: string
      Description: string option
      RawStatus: string
      Dependencies: string list
      RelatedStoryId: string option
      SourceLocation: SourceLocation option
      Metadata: Map<string, string> }

type TaskEdge =
    { FromTaskId: string
      ToTaskId: string }

type TaskGraph =
    { SelectedStoryId: string option
      Nodes: SpeckitTask list
      Edges: TaskEdge list
      Diagnostics: Diagnostic list
      SelectedTaskId: string option }

type PaneKind =
    | Features
    | Stories
    | Plan
    | TaskGraph
    | Details
    | Diagnostics

type Pane =
    { Id: string
      Title: string
      Kind: PaneKind
      IsFocused: bool
      ScrollOffset: int
      Selection: string option }

type DashboardLayoutMode =
    | Auto
    | Widescreen
    | Vertical

type DashboardResolvedLayout =
    | WidescreenLayout
    | VerticalLayout

type DashboardColorRole =
    | Selected
    | LastActivity
    | ProgressComplete
    | ProgressIncomplete
    | DiagnosticInfo
    | DiagnosticWarning
    | DiagnosticError
    | Muted
    | PanelAccent

type DashboardColorStyle =
    { Foreground: string
      Background: string option }

type DashboardUiPreferences =
    { Layout: DashboardLayoutMode
      Colors: Map<DashboardColorRole, DashboardColorStyle> }

type DashboardSnapshot =
    { RepositoryRoot: string
      CurrentBranch: string option
      Features: Feature list
      SelectedFeatureId: string option
      Stories: UserStory list
      SelectedStoryId: string option
      Plan: Plan option
      TaskGraph: TaskGraph option
      SelectedTaskId: string option
      Panes: Pane list
      Ui: DashboardUiPreferences
      Diagnostics: Diagnostic list
      LastRefreshedAt: DateTimeOffset }

module Domain =
    let diagnostic severity message source =
        { Severity = severity
          Message = message
          Source = source }

    let emptyFeatureStatus refreshedAt =
        { SpecState = Missing
          PlanState = Missing
          TasksState = Missing
          ChecklistState = Missing
          Diagnostics = []
          LastRefreshedAt = refreshedAt }

    let defaultPanes =
        [ { Id = "features"; Title = "Features"; Kind = Features; IsFocused = true; ScrollOffset = 0; Selection = None }
          { Id = "stories"; Title = "Stories"; Kind = Stories; IsFocused = false; ScrollOffset = 0; Selection = None }
          { Id = "plan"; Title = "Plan"; Kind = Plan; IsFocused = false; ScrollOffset = 0; Selection = None }
          { Id = "tasks"; Title = "Tasks"; Kind = TaskGraph; IsFocused = false; ScrollOffset = 0; Selection = None }
          { Id = "details"; Title = "Details"; Kind = Details; IsFocused = false; ScrollOffset = 0; Selection = None }
          { Id = "diagnostics"; Title = "Diagnostics"; Kind = Diagnostics; IsFocused = false; ScrollOffset = 0; Selection = None } ]

    let colorRoleId role =
        match role with
        | Selected -> "selected"
        | LastActivity -> "lastActivity"
        | ProgressComplete -> "progressComplete"
        | ProgressIncomplete -> "progressIncomplete"
        | DiagnosticInfo -> "diagnosticInfo"
        | DiagnosticWarning -> "diagnosticWarning"
        | DiagnosticError -> "diagnosticError"
        | Muted -> "muted"
        | PanelAccent -> "panelAccent"

    let tryColorRole text =
        match text with
        | "selected" -> Some Selected
        | "lastActivity" -> Some LastActivity
        | "progressComplete" -> Some ProgressComplete
        | "progressIncomplete" -> Some ProgressIncomplete
        | "diagnosticInfo" -> Some DiagnosticInfo
        | "diagnosticWarning" -> Some DiagnosticWarning
        | "diagnosticError" -> Some DiagnosticError
        | "muted" -> Some Muted
        | "panelAccent" -> Some PanelAccent
        | _ -> None

    let defaultUiPreferences =
        { Layout = Auto
          Colors =
            [ Selected, { Foreground = "black"; Background = Some "deepskyblue1" }
              LastActivity, { Foreground = "white"; Background = Some "grey23" }
              ProgressComplete, { Foreground = "green"; Background = None }
              ProgressIncomplete, { Foreground = "grey"; Background = None }
              DiagnosticInfo, { Foreground = "deepskyblue1"; Background = None }
              DiagnosticWarning, { Foreground = "yellow"; Background = None }
              DiagnosticError, { Foreground = "red"; Background = None }
              Muted, { Foreground = "grey"; Background = None }
              PanelAccent, { Foreground = "deepskyblue1"; Background = None } ]
            |> Map.ofList }

    let resolveLayout terminalColumns mode =
        match mode with
        | Widescreen -> WidescreenLayout
        | Vertical -> VerticalLayout
        | Auto when terminalColumns >= 120 -> WidescreenLayout
        | Auto -> VerticalLayout
