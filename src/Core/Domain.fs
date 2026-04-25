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
