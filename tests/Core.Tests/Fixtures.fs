module SkDashboard.Core.Tests.Fixtures

open System.IO

type SpeckitProjectFixture =
    { Root: string
      FeatureRoot: string option }

let emptyProject () =
    { Root = Directory.CreateTempSubdirectory("sk-dashboard-fixture-empty-").FullName
      FeatureRoot = None }

let emptySpecsProject () =
    let root = Directory.CreateTempSubdirectory("sk-dashboard-fixture-empty-specs-").FullName
    Directory.CreateDirectory(Path.Combine(root, "specs")) |> ignore
    { Root = root; FeatureRoot = None }

let partialProject () =
    let root = Directory.CreateTempSubdirectory("sk-dashboard-fixture-partial-").FullName
    let featureRoot = Path.Combine(root, "specs", "001-partial")
    Directory.CreateDirectory(featureRoot) |> ignore
    File.WriteAllText(Path.Combine(featureRoot, "spec.md"), "# Partial\n\n### User Story 1 - Partial (Priority: P1)")
    { Root = root; FeatureRoot = Some featureRoot }

let completeProject () =
    let root = Directory.CreateTempSubdirectory("sk-dashboard-fixture-complete-").FullName
    let featureRoot = Path.Combine(root, "specs", "001-complete")
    Directory.CreateDirectory(Path.Combine(featureRoot, "checklists")) |> ignore
    File.WriteAllText(Path.Combine(featureRoot, "spec.md"), "# Complete\n\n### User Story 1 - Complete (Priority: P1)")
    File.WriteAllText(Path.Combine(featureRoot, "plan.md"), "# Plan\n\n## Summary\nComplete")
    File.WriteAllText(Path.Combine(featureRoot, "tasks.md"), "- [X] T001 [US1] Complete")
    File.WriteAllText(Path.Combine(featureRoot, "checklists", "requirements.md"), "- [x] Complete")
    { Root = root; FeatureRoot = Some featureRoot }

let malformedProject () =
    let root = Directory.CreateTempSubdirectory("sk-dashboard-fixture-malformed-").FullName
    let featureRoot = Path.Combine(root, "specs", "001-malformed")
    Directory.CreateDirectory(featureRoot) |> ignore
    File.WriteAllText(Path.Combine(featureRoot, "spec.md"), "# Missing stories")
    File.WriteAllText(Path.Combine(featureRoot, "plan.md"), "# Missing summary")
    File.WriteAllText(Path.Combine(featureRoot, "tasks.md"), "not a task row")
    { Root = root; FeatureRoot = Some featureRoot }

let tempConfigFile name =
    let root = Directory.CreateTempSubdirectory("sk-dashboard-config-" + name + "-").FullName
    Path.Combine(root, "dashboard.json")

let themeFolders (configPath: string) =
    let root = Path.GetDirectoryName configPath |> Option.ofObj |> Option.defaultValue "."
    let app = Path.Combine(root, "themes", "app")
    let markdown = Path.Combine(root, "themes", "markdown")
    Directory.CreateDirectory(app) |> ignore
    Directory.CreateDirectory(markdown) |> ignore
    app, markdown

let writeThemeFile (folder: string) (name: string) (content: string) =
    let path = Path.Combine(folder, name)
    File.WriteAllText(path, content)
    path

let checklistProject (files: (string * string) list) =
    let root = Directory.CreateTempSubdirectory("sk-dashboard-checklist-fixture-").FullName
    let featureRoot = Path.Combine(root, "specs", "001-checklists")
    let checklistRoot = Path.Combine(featureRoot, "checklists")
    Directory.CreateDirectory(checklistRoot) |> ignore
    File.WriteAllText(Path.Combine(featureRoot, "spec.md"), "### User Story 1 - Checklists (Priority: P1)")
    File.WriteAllText(Path.Combine(featureRoot, "plan.md"), "## Summary\nChecklist fixture")
    File.WriteAllText(Path.Combine(featureRoot, "tasks.md"), "- [ ] T001 [US1] Checklist task")

    for name, content in files do
        File.WriteAllText(Path.Combine(checklistRoot, name), content)

    { Root = root; FeatureRoot = Some featureRoot }
