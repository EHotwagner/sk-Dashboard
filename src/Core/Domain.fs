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

type ResolvedDisplayMode =
    | LightDisplayMode
    | DarkDisplayMode

type ThemeFamily =
    | AppThemeFamily
    | MarkdownThemeFamily

type ThemeSource =
    | BuiltInTheme
    | CustomTheme of string
    | FallbackTheme of string

type ThemeValidationStatus =
    | ThemeValid
    | ThemeValidWithReplacements
    | ThemeIgnored of string
    | ThemeDuplicate of string
    | ThemeIncomplete of string
    | ThemeUnreadable of string
    | ThemeWrongFamily of ThemeFamily
    | ThemeUnavailable of string

type ThemeValidationFeedback =
    { Severity: DiagnosticSeverity
      Family: ThemeFamily
      ThemeId: string option
      Source: string option
      Message: string
      FailureKind: string }

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
      HorizontalStep: int
      AlternateRowShading: bool }

type DashboardDetailPreferences = { WrapText: bool; HorizontalStep: int }

type DashboardLiveReloadPreferences =
    { Enabled: bool
      DebounceMilliseconds: int }

type MarkdownThemeColors =
    { Normal: string
      Heading: string
      Emphasis: string
      Strong: string
      Link: string
      InlineCode: string
      CodeBlock: string
      BlockQuote: string
      ListMarker: string
      CheckedItem: string
      UncheckedItem: string
      Note: string
      Muted: string }

type MarkdownThemeSpacing =
    { BeforeHeading: int
      AfterHeading: int
      BetweenParagraphs: int
      AroundCodeBlock: int
      AroundList: int }

type MarkdownThemePresentation =
    { Id: string
      DisplayName: string
      Colors: MarkdownThemeColors
      Spacing: MarkdownThemeSpacing }

type DashboardUiPreferences =
    { Layout: DashboardLayoutMode
      Table: DashboardTablePreferences
      Detail: DashboardDetailPreferences
      LiveReload: DashboardLiveReloadPreferences
      Themes: ThemeSelection
      Markdown: MarkdownThemePresentation
      Colors: Map<DashboardColorRole, DashboardColorStyle> }

and ThemeSelection =
    { SelectedAppThemeId: string
      SelectedMarkdownThemeId: string
      AvailableAppThemes: string list
      AvailableMarkdownThemes: string list
      AppThemeFallback: bool
      MarkdownThemeFallback: bool }

type AppTheme =
    { Id: string
      DisplayName: string
      Source: ThemeSource
      Mode: ResolvedDisplayMode option
      Table: DashboardTablePreferences
      AlternateRowShading: bool
      Colors: Map<DashboardColorRole, DashboardColorStyle>
      ValidationStatus: ThemeValidationStatus
      Diagnostics: ThemeValidationFeedback list }

type MarkdownTheme =
    { Id: string
      DisplayName: string
      Source: ThemeSource
      ModeCompatibility: ResolvedDisplayMode option
      Colors: MarkdownThemeColors
      Spacing: MarkdownThemeSpacing
      ValidationStatus: ThemeValidationStatus
      Diagnostics: ThemeValidationFeedback list }

type ThemeCatalog =
    { AppThemes: AppTheme list
      MarkdownThemes: MarkdownTheme list
      Diagnostics: ThemeValidationFeedback list }

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
    | ChecklistFullScreen
    | SettingsFullScreen

type TableViewport =
    { RowOffset: int
      ColumnOffset: int
      VisibleRows: int
      VisibleColumns: int
      StickyColumns: int }

type DetailViewport =
    { LineOffset: int
      ColumnOffset: int
      VisibleLines: int
      VisibleColumns: int }

type ChecklistArtifact =
    { Id: string
      DisplayName: string
      Path: string }

type DashboardContext =
    { SelectedFeatureId: string option
      SelectedStoryId: string option
      SelectedTaskId: string option
      FocusedPaneId: string option
      PreviousFullScreenTarget: FullScreenTarget option }

type ChecklistViewMode =
    | ChecklistListing
    | ChecklistReading
    | ChecklistEmpty
    | ChecklistError

type ChecklistViewState =
    { AvailableChecklists: ChecklistArtifact list
      SelectedChecklist: ChecklistArtifact option
      Document: MarkdownDocument option
      PreviousContext: DashboardContext
      Viewport: DetailViewport
      Diagnostics: Diagnostic list
      Mode: ChecklistViewMode }

type FullScreenModal =
    { Target: FullScreenTarget
      SelectedFeatureId: string option
      SelectedStoryId: string option
      SelectedTaskId: string option
      Document: MarkdownDocument option
      Checklist: ChecklistViewState option
      Viewport: DetailViewport }

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

    let defaultThemeSelection =
        { SelectedAppThemeId = "default"
          SelectedMarkdownThemeId = "default"
          AvailableAppThemes =
            [ "default"
              "light"
              "dark"
              "dracula-dark"
              "nord-dark"
              "tokyo-night"
              "solarized-light"
              "github-light"
              "gruvbox-light" ]
          AvailableMarkdownThemes =
            [ "plain"
              "default"
              "dracula-dark"
              "nord-dark"
              "tokyo-night"
              "solarized-light"
              "github-light"
              "gruvbox-light" ]
          AppThemeFallback = false
          MarkdownThemeFallback = false }

    let themeFamilyId family =
        match family with
        | AppThemeFamily -> "app"
        | MarkdownThemeFamily -> "markdown"

    let markdownColors normal heading emphasis strong link inlineCode codeBlock blockQuote listMarker checkedItem uncheckedItem note muted =
        { Normal = normal
          Heading = heading
          Emphasis = emphasis
          Strong = strong
          Link = link
          InlineCode = inlineCode
          CodeBlock = codeBlock
          BlockQuote = blockQuote
          ListMarker = listMarker
          CheckedItem = checkedItem
          UncheckedItem = uncheckedItem
          Note = note
          Muted = muted }

    let markdownSpacing beforeHeading afterHeading betweenParagraphs aroundCodeBlock aroundList =
        { BeforeHeading = beforeHeading
          AfterHeading = afterHeading
          BetweenParagraphs = betweenParagraphs
          AroundCodeBlock = aroundCodeBlock
          AroundList = aroundList }

    let defaultMarkdownColors =
        markdownColors
            "white"
            "deepskyblue1"
            "white"
            "white"
            "cyan"
            "yellow"
            "grey"
            "grey"
            "deepskyblue1"
            "green"
            "yellow"
            "magenta"
            "grey"

    let defaultMarkdownSpacing = markdownSpacing 0 0 1 1 0

    let defaultMarkdownPresentation =
        { Id = "default"
          DisplayName = "Default"
          Colors = defaultMarkdownColors
          Spacing = defaultMarkdownSpacing }

    let defaultUiPreferences =
        { Layout = Auto
          Table =
            { Border = RoundedBorder
              StickyColumns = 1
              HorizontalStep = 8
              AlternateRowShading = false }
          Detail = { WrapText = false; HorizontalStep = 8 }
          LiveReload =
            { Enabled = true
              DebounceMilliseconds = 250 }
          Themes = defaultThemeSelection
          Markdown = defaultMarkdownPresentation
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

    let appTheme id displayName mode table alternateRowShading colors =
        { Id = id
          DisplayName = displayName
          Source = BuiltInTheme
          Mode = mode
          Table = { table with AlternateRowShading = alternateRowShading }
          AlternateRowShading = alternateRowShading
          Colors = colors
          ValidationStatus = ThemeValid
          Diagnostics = [] }

    let builtInAppThemes =
        let defaults = defaultUiPreferences

        let fg foreground =
            { Foreground = foreground
              Background = None }

        let pair foreground background =
            { Foreground = foreground
              Background = Some background }

        let withColors entries =
            entries
            |> List.fold (fun colors (role, style) -> colors |> Map.add role style) defaults.Colors

        let lightColors =
            withColors
                [ Selected, pair "black" "deepskyblue1"
                  Muted, fg "grey42"
                  PanelAccent, fg "blue"
                  RowStripeOdd, pair "black" "white"
                  RowStripeEven, pair "black" "white"
                  DetailBody, fg "black" ]

        let darkColors =
            withColors
                [ Selected, pair "black" "deepskyblue1"
                  Muted, fg "grey"
                  PanelAccent, fg "deepskyblue1"
                  RowStripeOdd, pair "white" "black"
                  RowStripeEven, pair "white" "black"
                  DetailBody, fg "white" ]

        let draculaDarkColors =
            withColors
                [ Selected, pair "black" "#ff79c6"
                  LastActivity, pair "#f8f8f2" "#44475a"
                  ProgressComplete, fg "#50fa7b"
                  ProgressIncomplete, fg "#6272a4"
                  DiagnosticInfo, fg "#8be9fd"
                  DiagnosticWarning, fg "#f1fa8c"
                  DiagnosticError, fg "#ff5555"
                  Muted, fg "#6272a4"
                  PanelAccent, fg "#bd93f9"
                  RowStripeOdd, pair "#f8f8f2" "#282a36"
                  RowStripeEven, pair "#f8f8f2" "#21222c"
                  DetailHeading, fg "#bd93f9"
                  DetailLabel, fg "#ffb86c"
                  DetailBody, fg "#f8f8f2"
                  DetailSource, fg "#6272a4" ]

        let nordDarkColors =
            withColors
                [ Selected, pair "black" "#88c0d0"
                  LastActivity, pair "#eceff4" "#3b4252"
                  ProgressComplete, fg "#a3be8c"
                  ProgressIncomplete, fg "#4c566a"
                  DiagnosticInfo, fg "#88c0d0"
                  DiagnosticWarning, fg "#ebcb8b"
                  DiagnosticError, fg "#bf616a"
                  Muted, fg "#81a1c1"
                  PanelAccent, fg "#8fbcbb"
                  RowStripeOdd, pair "#eceff4" "#2e3440"
                  RowStripeEven, pair "#eceff4" "#3b4252"
                  DetailHeading, fg "#88c0d0"
                  DetailLabel, fg "#ebcb8b"
                  DetailBody, fg "#eceff4"
                  DetailSource, fg "#81a1c1" ]

        let tokyoNightColors =
            withColors
                [ Selected, pair "black" "#7aa2f7"
                  LastActivity, pair "#c0caf5" "#292e42"
                  ProgressComplete, fg "#9ece6a"
                  ProgressIncomplete, fg "#565f89"
                  DiagnosticInfo, fg "#7dcfff"
                  DiagnosticWarning, fg "#e0af68"
                  DiagnosticError, fg "#f7768e"
                  Muted, fg "#565f89"
                  PanelAccent, fg "#bb9af7"
                  RowStripeOdd, pair "#c0caf5" "#1a1b26"
                  RowStripeEven, pair "#c0caf5" "#24283b"
                  DetailHeading, fg "#7aa2f7"
                  DetailLabel, fg "#e0af68"
                  DetailBody, fg "#c0caf5"
                  DetailSource, fg "#565f89" ]

        let solarizedLightColors =
            withColors
                [ Selected, pair "#002b36" "#b58900"
                  LastActivity, pair "#073642" "#eee8d5"
                  ProgressComplete, fg "#859900"
                  ProgressIncomplete, fg "#93a1a1"
                  DiagnosticInfo, fg "#268bd2"
                  DiagnosticWarning, fg "#b58900"
                  DiagnosticError, fg "#dc322f"
                  Muted, fg "#657b83"
                  PanelAccent, fg "#268bd2"
                  RowStripeOdd, pair "#073642" "#fdf6e3"
                  RowStripeEven, pair "#073642" "#eee8d5"
                  DetailHeading, fg "#268bd2"
                  DetailLabel, fg "#b58900"
                  DetailBody, fg "#073642"
                  DetailSource, fg "#657b83" ]

        let githubLightColors =
            withColors
                [ Selected, pair "white" "#0969da"
                  LastActivity, pair "#24292f" "#f6f8fa"
                  ProgressComplete, fg "#1a7f37"
                  ProgressIncomplete, fg "#6e7781"
                  DiagnosticInfo, fg "#0969da"
                  DiagnosticWarning, fg "#9a6700"
                  DiagnosticError, fg "#cf222e"
                  Muted, fg "#6e7781"
                  PanelAccent, fg "#0969da"
                  RowStripeOdd, pair "#24292f" "#ffffff"
                  RowStripeEven, pair "#24292f" "#f6f8fa"
                  DetailHeading, fg "#0969da"
                  DetailLabel, fg "#9a6700"
                  DetailBody, fg "#24292f"
                  DetailSource, fg "#6e7781" ]

        let gruvboxLightColors =
            withColors
                [ Selected, pair "#fbf1c7" "#af3a03"
                  LastActivity, pair "#3c3836" "#ebdbb2"
                  ProgressComplete, fg "#79740e"
                  ProgressIncomplete, fg "#928374"
                  DiagnosticInfo, fg "#076678"
                  DiagnosticWarning, fg "#b57614"
                  DiagnosticError, fg "#9d0006"
                  Muted, fg "#7c6f64"
                  PanelAccent, fg "#076678"
                  RowStripeOdd, pair "#3c3836" "#fbf1c7"
                  RowStripeEven, pair "#3c3836" "#f2e5bc"
                  DetailHeading, fg "#076678"
                  DetailLabel, fg "#b57614"
                  DetailBody, fg "#3c3836"
                  DetailSource, fg "#7c6f64" ]

        [ appTheme "default" "Default" None defaults.Table false defaults.Colors
          appTheme "light" "Light" (Some LightDisplayMode) defaults.Table false lightColors
          appTheme "dark" "Dark" (Some DarkDisplayMode) defaults.Table false darkColors
          appTheme "dracula-dark" "Dracula Dark" (Some DarkDisplayMode) defaults.Table false draculaDarkColors
          appTheme "nord-dark" "Nord Dark" (Some DarkDisplayMode) defaults.Table false nordDarkColors
          appTheme "tokyo-night" "Tokyo Night" (Some DarkDisplayMode) defaults.Table false tokyoNightColors
          appTheme "solarized-light" "Solarized Light" (Some LightDisplayMode) defaults.Table false solarizedLightColors
          appTheme "github-light" "GitHub Light" (Some LightDisplayMode) defaults.Table false githubLightColors
          appTheme "gruvbox-light" "Gruvbox Light" (Some LightDisplayMode) defaults.Table false gruvboxLightColors ]

    let builtInMarkdownThemes =
        let markdownTheme id displayName mode colors spacing =
            { Id = id
              DisplayName = displayName
              Source = BuiltInTheme
              ModeCompatibility = mode
              Colors = colors
              Spacing = spacing
              ValidationStatus = ThemeValid
              Diagnostics = [] }

        let plain =
            markdownTheme
                "plain"
                "Plain"
                None
                (markdownColors
                    "white"
                    "white"
                    "white"
                    "white"
                    "white"
                    "yellow"
                    "grey"
                    "grey"
                    "white"
                    "green"
                    "yellow"
                    "grey"
                    "grey")
                (markdownSpacing 0 0 0 0 0)

        let readable =
            markdownTheme "default" "Default" None defaultMarkdownColors defaultMarkdownSpacing

        let comfortableSpacing = markdownSpacing 0 0 1 1 0

        let draculaDark =
            markdownTheme
                "dracula-dark"
                "Dracula Dark"
                (Some DarkDisplayMode)
                (markdownColors
                    "#f8f8f2"
                    "#bd93f9"
                    "#ff79c6"
                    "#f8f8f2"
                    "#8be9fd"
                    "#f1fa8c"
                    "#6272a4"
                    "#6272a4"
                    "#ffb86c"
                    "#50fa7b"
                    "#ff5555"
                    "#ff79c6"
                    "#6272a4")
                comfortableSpacing

        let nordDark =
            markdownTheme
                "nord-dark"
                "Nord Dark"
                (Some DarkDisplayMode)
                (markdownColors
                    "#eceff4"
                    "#88c0d0"
                    "#b48ead"
                    "#eceff4"
                    "#81a1c1"
                    "#ebcb8b"
                    "#4c566a"
                    "#81a1c1"
                    "#8fbcbb"
                    "#a3be8c"
                    "#bf616a"
                    "#d08770"
                    "#81a1c1")
                comfortableSpacing

        let tokyoNight =
            markdownTheme
                "tokyo-night"
                "Tokyo Night"
                (Some DarkDisplayMode)
                (markdownColors
                    "#c0caf5"
                    "#7aa2f7"
                    "#bb9af7"
                    "#c0caf5"
                    "#7dcfff"
                    "#e0af68"
                    "#565f89"
                    "#565f89"
                    "#bb9af7"
                    "#9ece6a"
                    "#f7768e"
                    "#e0af68"
                    "#565f89")
                comfortableSpacing

        let solarizedLight =
            markdownTheme
                "solarized-light"
                "Solarized Light"
                (Some LightDisplayMode)
                (markdownColors
                    "#073642"
                    "#268bd2"
                    "#d33682"
                    "#002b36"
                    "#268bd2"
                    "#b58900"
                    "#657b83"
                    "#93a1a1"
                    "#2aa198"
                    "#859900"
                    "#dc322f"
                    "#cb4b16"
                    "#657b83")
                comfortableSpacing

        let githubLight =
            markdownTheme
                "github-light"
                "GitHub Light"
                (Some LightDisplayMode)
                (markdownColors
                    "#24292f"
                    "#0969da"
                    "#8250df"
                    "#24292f"
                    "#0969da"
                    "#9a6700"
                    "#6e7781"
                    "#6e7781"
                    "#0969da"
                    "#1a7f37"
                    "#cf222e"
                    "#bf3989"
                    "#6e7781")
                comfortableSpacing

        let gruvboxLight =
            markdownTheme
                "gruvbox-light"
                "Gruvbox Light"
                (Some LightDisplayMode)
                (markdownColors
                    "#3c3836"
                    "#076678"
                    "#8f3f71"
                    "#3c3836"
                    "#076678"
                    "#b57614"
                    "#7c6f64"
                    "#928374"
                    "#427b58"
                    "#79740e"
                    "#9d0006"
                    "#af3a03"
                    "#7c6f64")
                comfortableSpacing

        [ plain
          readable
          draculaDark
          nordDark
          tokyoNight
          solarizedLight
          githubLight
          gruvboxLight ]

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
