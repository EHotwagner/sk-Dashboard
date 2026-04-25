namespace SkDashboard.Core

open System
open System.Reflection

type DiagnosticSeverity =
    | Info
    | Warning
    | Error

type SourceLocation = { Path: string; Line: int option }

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
    { FromTaskId: string; ToTaskId: string }

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
    | RowStripeOdd
    | RowStripeEven
    | DetailHeading
    | DetailLabel
    | DetailBody
    | DetailSource

type DashboardColorStyle =
    { Foreground: string
      Background: string option }

type TableBorderStyle =
    | NoBorder
    | MinimalBorder
    | RoundedBorder
    | HeavyBorder

type DashboardTablePreferences =
    { Border: TableBorderStyle
      StickyColumns: int
      HorizontalStep: int }

type DashboardDetailPreferences = { WrapText: bool; HorizontalStep: int }

type DashboardLiveReloadPreferences =
    { Enabled: bool
      DebounceMilliseconds: int }

type DashboardUiPreferences =
    { Layout: DashboardLayoutMode
      Table: DashboardTablePreferences
      Detail: DashboardDetailPreferences
      LiveReload: DashboardLiveReloadPreferences
      Colors: Map<DashboardColorRole, DashboardColorStyle> }

type DashboardVersionDisplay =
    { Label: string
      Source: string
      Diagnostic: Diagnostic option }

type MarkdownRenderStatus =
    | MarkdownRendered
    | MarkdownPlainTextFallback of string
    | MarkdownEmptyDocument
    | MarkdownSourceMissing
    | MarkdownSourceUnreadable of string

type MarkdownDocumentSource = { Path: string; DisplayName: string }

type RenderFailureDiagnostic =
    { Message: string
      SourcePath: string option }

type MarkdownDocument =
    { Title: string
      RawContent: string
      Source: MarkdownDocumentSource option
      Status: MarkdownRenderStatus
      Diagnostics: RenderFailureDiagnostic list }

type FullScreenTarget =
    | FeatureFullScreen
    | StoryFullScreen
    | PlanFullScreen
    | TaskFullScreen
    | ConstitutionFullScreen
    | SettingsFullScreen

type FullScreenModal =
    { Target: FullScreenTarget
      SelectedFeatureId: string option
      SelectedStoryId: string option
      SelectedTaskId: string option
      Document: MarkdownDocument option
      Viewport: DetailViewport }

and TableViewport =
    { RowOffset: int
      ColumnOffset: int
      VisibleRows: int
      VisibleColumns: int
      StickyColumns: int }

and DetailViewport =
    { LineOffset: int
      ColumnOffset: int
      VisibleLines: int
      VisibleColumns: int }

type ConfigFileVersion =
    { Path: string
      LastWriteTimeUtc: DateTimeOffset option
      Length: int64 option }

type SettingsConflict =
    | NoSettingsConflict
    | StaleSettingsSave of ConfigFileVersion
    | PendingExternalReload of ConfigFileVersion

type SettingsEditSession =
    { Loaded: DashboardUiPreferences
      Draft: DashboardUiPreferences
      LoadedVersion: ConfigFileVersion
      Dirty: bool
      Conflict: SettingsConflict }

type LiveReloadState =
    { LastValid: DashboardUiPreferences
      LastVersion: ConfigFileVersion option
      PendingVersion: ConfigFileVersion option
      Diagnostics: Diagnostic list }

type DashboardSnapshot =
    { RepositoryRoot: string
      CurrentBranch: string option
      Version: DashboardVersionDisplay
      Features: Feature list
      SelectedFeatureId: string option
      Stories: UserStory list
      SelectedStoryId: string option
      Plan: Plan option
      TaskGraph: TaskGraph option
      SelectedTaskId: string option
      Panes: Pane list
      Ui: DashboardUiPreferences
      FullScreen: FullScreenModal option
      Diagnostics: Diagnostic list
      LastRefreshedAt: DateTimeOffset }

module Domain =
    let diagnostic severity message source =
        { Severity = severity
          Message = message
          Source = source }

    let constitutionRelativePath = ".specify/memory/constitution.md"

    let emptyFeatureStatus refreshedAt =
        { SpecState = Missing
          PlanState = Missing
          TasksState = Missing
          ChecklistState = Missing
          Diagnostics = []
          LastRefreshedAt = refreshedAt }

    let defaultPanes =
        [ { Id = "features"
            Title = "Features"
            Kind = Features
            IsFocused = true
            ScrollOffset = 0
            Selection = None }
          { Id = "stories"
            Title = "Stories"
            Kind = Stories
            IsFocused = false
            ScrollOffset = 0
            Selection = None }
          { Id = "plan"
            Title = "Plan"
            Kind = Plan
            IsFocused = false
            ScrollOffset = 0
            Selection = None }
          { Id = "tasks"
            Title = "Tasks"
            Kind = TaskGraph
            IsFocused = false
            ScrollOffset = 0
            Selection = None }
          { Id = "details"
            Title = "Details"
            Kind = Details
            IsFocused = false
            ScrollOffset = 0
            Selection = None }
          { Id = "diagnostics"
            Title = "Diagnostics"
            Kind = Diagnostics
            IsFocused = false
            ScrollOffset = 0
            Selection = None } ]

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
        | RowStripeOdd -> "rowStripeOdd"
        | RowStripeEven -> "rowStripeEven"
        | DetailHeading -> "detailHeading"
        | DetailLabel -> "detailLabel"
        | DetailBody -> "detailBody"
        | DetailSource -> "detailSource"

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
        | "rowStripeOdd" -> Some RowStripeOdd
        | "rowStripeEven" -> Some RowStripeEven
        | "detailHeading" -> Some DetailHeading
        | "detailLabel" -> Some DetailLabel
        | "detailBody" -> Some DetailBody
        | "detailSource" -> Some DetailSource
        | _ -> None

    let tableBorderId border =
        match border with
        | NoBorder -> "none"
        | MinimalBorder -> "minimal"
        | RoundedBorder -> "rounded"
        | HeavyBorder -> "heavy"

    let tryTableBorder (text: string) =
        match text.Trim().ToLowerInvariant() with
        | "none" -> Some NoBorder
        | "minimal" -> Some MinimalBorder
        | "rounded" -> Some RoundedBorder
        | "heavy" -> Some HeavyBorder
        | _ -> None

    let defaultUiPreferences =
        { Layout = Auto
          Table =
            { Border = RoundedBorder
              StickyColumns = 1
              HorizontalStep = 8 }
          Detail = { WrapText = false; HorizontalStep = 8 }
          LiveReload =
            { Enabled = true
              DebounceMilliseconds = 250 }
          Colors =
            [ Selected,
              { Foreground = "black"
                Background = Some "deepskyblue1" }
              LastActivity,
              { Foreground = "white"
                Background = Some "grey23" }
              ProgressComplete,
              { Foreground = "green"
                Background = None }
              ProgressIncomplete,
              { Foreground = "grey"
                Background = None }
              DiagnosticInfo,
              { Foreground = "deepskyblue1"
                Background = None }
              DiagnosticWarning,
              { Foreground = "yellow"
                Background = None }
              DiagnosticError,
              { Foreground = "red"
                Background = None }
              Muted,
              { Foreground = "grey"
                Background = None }
              PanelAccent,
              { Foreground = "deepskyblue1"
                Background = None }
              RowStripeOdd,
              { Foreground = "white"
                Background = Some "grey7" }
              RowStripeEven,
              { Foreground = "white"
                Background = Some "black" }
              DetailHeading,
              { Foreground = "deepskyblue1"
                Background = None }
              DetailLabel,
              { Foreground = "yellow"
                Background = None }
              DetailBody,
              { Foreground = "white"
                Background = None }
              DetailSource,
              { Foreground = "grey"
                Background = None } ]
            |> Map.ofList }

    let clamp minValue maxValue value = value |> max minValue |> min maxValue

    let defaultTableViewport visibleRows visibleColumns =
        { RowOffset = 0
          ColumnOffset = 0
          VisibleRows = max 1 visibleRows
          VisibleColumns = max 1 visibleColumns
          StickyColumns = 1 }

    let defaultDetailViewport visibleLines visibleColumns =
        { LineOffset = 0
          ColumnOffset = 0
          VisibleLines = max 1 visibleLines
          VisibleColumns = max 1 visibleColumns }

    let clampTableViewport rowCount columnCount viewport =
        let visibleRows = max 1 viewport.VisibleRows
        let visibleColumns = max 1 viewport.VisibleColumns
        let maxRowOffset = max 0 (rowCount - visibleRows)
        let maxColumnOffset = max 0 (columnCount - visibleColumns)

        { viewport with
            RowOffset = clamp 0 maxRowOffset viewport.RowOffset
            ColumnOffset = clamp 0 maxColumnOffset viewport.ColumnOffset
            VisibleRows = visibleRows
            VisibleColumns = visibleColumns
            StickyColumns = clamp 0 visibleColumns viewport.StickyColumns }

    let keepRowVisible selectedIndex rowCount viewport =
        let normalized = clampTableViewport rowCount 1 viewport
        let selected = clamp 0 (max 0 (rowCount - 1)) selectedIndex

        let rowOffset =
            if rowCount <= 0 then
                0
            elif selected < normalized.RowOffset then
                selected
            elif selected >= normalized.RowOffset + normalized.VisibleRows then
                selected - normalized.VisibleRows + 1
            else
                normalized.RowOffset

        clampTableViewport
            rowCount
            1
            { normalized with
                RowOffset = rowOffset }

    let scrollTableColumns delta columnCount viewport =
        clampTableViewport
            1
            columnCount
            { viewport with
                ColumnOffset = viewport.ColumnOffset + delta }

    let resizeTableViewport visibleRows visibleColumns rowCount columnCount viewport =
        clampTableViewport
            rowCount
            columnCount
            { viewport with
                VisibleRows = visibleRows
                VisibleColumns = visibleColumns }

    let clampDetailViewport lineCount columnCount viewport =
        let visibleLines = max 1 viewport.VisibleLines
        let visibleColumns = max 1 viewport.VisibleColumns
        let maxLineOffset = max 0 (lineCount - visibleLines)
        let maxColumnOffset = max 0 (columnCount - visibleColumns)

        { viewport with
            LineOffset = clamp 0 maxLineOffset viewport.LineOffset
            ColumnOffset = clamp 0 maxColumnOffset viewport.ColumnOffset
            VisibleLines = visibleLines
            VisibleColumns = visibleColumns }

    let scrollDetailLines delta lineCount viewport =
        clampDetailViewport
            lineCount
            1
            { viewport with
                LineOffset = viewport.LineOffset + delta }

    let scrollDetailColumns delta columnCount viewport =
        clampDetailViewport
            1
            columnCount
            { viewport with
                ColumnOffset = viewport.ColumnOffset + delta }

    let settingsSession settings version =
        { Loaded = settings
          Draft = settings
          LoadedVersion = version
          Dirty = false
          Conflict = NoSettingsConflict }

    let updateSettingsDraft draft session =
        { session with
            Draft = draft
            Dirty = draft <> session.Loaded }

    let markSettingsConflict version session =
        { session with
            Conflict = StaleSettingsSave version }

    let discardSettingsDraft session =
        { session with
            Draft = session.Loaded
            Dirty = false
            Conflict = NoSettingsConflict }

    let reloadSettingsDraft settings version (_session: SettingsEditSession) = settingsSession settings version

    let liveReloadState settings =
        { LastValid = settings
          LastVersion = None
          PendingVersion = None
          Diagnostics = [] }

    let applyLiveReload version settings state =
        { state with
            LastValid = settings
            LastVersion = Some version
            PendingVersion = None
            Diagnostics = [] }

    let deferLiveReload version state =
        { state with
            PendingVersion = Some version }

    let normalizeDashboardVersionLabel (value: string) =
        let trimmed = value.Trim()

        let withoutBuildMetadata =
            let metadataIndex = trimmed.IndexOf('+')

            if metadataIndex >= 0 then
                trimmed.Substring(0, metadataIndex)
            else
                trimmed

        let normalized = withoutBuildMetadata.Trim().TrimStart('v', 'V')

        if String.IsNullOrWhiteSpace normalized then
            None
        else
            Some("v" + normalized)

    let resolveDashboardVersion () =
        let assembly =
            Assembly.GetEntryAssembly()
            |> Option.ofObj
            |> Option.defaultValue typeof<DashboardSnapshot>.Assembly

        let informational =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            |> Option.ofObj
            |> Option.bind (fun attribute -> normalizeDashboardVersionLabel attribute.InformationalVersion)

        let assemblyVersion =
            assembly.GetName().Version
            |> Option.ofObj
            |> Option.bind (fun version ->
                let text =
                    if version.Revision > 0 then
                        version.ToString()
                    elif version.Build > 0 then
                        sprintf "%d.%d.%d" version.Major version.Minor version.Build
                    else
                        sprintf "%d.%d" version.Major version.Minor

                normalizeDashboardVersionLabel text)

        match informational |> Option.orElse assemblyVersion with
        | Some label ->
            { Label = label
              Source = assembly.GetName().Name |> Option.ofObj |> Option.defaultValue "assembly"
              Diagnostic = None }
        | None ->
            let diagnostic =
                diagnostic Warning "Dashboard version metadata is unavailable; showing vunknown." None

            { Label = "vunknown"
              Source = "fallback"
              Diagnostic = Some diagnostic }

    let resolveLayout terminalColumns mode =
        match mode with
        | Widescreen -> WidescreenLayout
        | Vertical -> VerticalLayout
        | Auto when terminalColumns >= 120 -> WidescreenLayout
        | Auto -> VerticalLayout
