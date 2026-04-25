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
