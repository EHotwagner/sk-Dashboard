module SkDashboard.Core.Tests.ArtifactParsingTests

open System
open System.IO
open Expecto
open SkDashboard.Core

[<Tests>]
let artifactParsingTests =
    testList "Speckit artifacts" [
        test "loadSnapshot_returns empty actionable state without specs directory" {
            let fixture = Fixtures.emptyProject ()
            let snapshot = SpeckitArtifacts.loadSnapshot fixture.Root
            Expect.isEmpty snapshot.Features "No features are expected."
            Expect.isNonEmpty snapshot.Diagnostics "Missing specs should produce an informational diagnostic."
        }

        test "loadSnapshot_returns empty actionable state with empty specs directory" {
            let fixture = Fixtures.emptySpecsProject ()
            let snapshot = SpeckitArtifacts.loadSnapshot fixture.Root
            Expect.isEmpty snapshot.Features "Empty specs directory has no features."
            Expect.isNonEmpty snapshot.Diagnostics "Empty specs should produce an informational diagnostic."
        }

        test "loadSnapshot_reports missing feature artifacts without throwing" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-missing-artifacts-").FullName
            Directory.CreateDirectory(Path.Combine(root, "specs", "001-empty-feature")) |> ignore
            let snapshot = SpeckitArtifacts.loadSnapshot root
            Expect.equal (snapshot.Features |> List.map _.Id) [ "001-empty-feature" ] "Feature directory is visible."
            Expect.isEmpty snapshot.Stories "Missing spec produces no stories."
            Expect.isNonEmpty snapshot.Diagnostics "Missing spec is reported."
        }

        test "parseUserStories_extracts story headings" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-spec-").FullName
            let path = Path.Combine(root, "spec.md")
            File.WriteAllText(path, "### User Story 1 - Open Dashboard (Priority: P1)\n\nA developer opens the dashboard.\n\n**Acceptance Scenarios**:\n\n1. **Given** a project, **When** opened, **Then** it renders.\n")
            let stories, diagnostics = SpeckitArtifacts.parseUserStories path
            Expect.isEmpty diagnostics "Valid story heading should not produce diagnostics."
            Expect.equal (stories |> List.map _.Id) [ "US1" ] "Story id is derived from heading order."
            Expect.equal stories.Head.Priority (Some "P1") "Priority is preserved."
            Expect.stringContains stories.Head.Description "developer opens" "Description is extracted."
            Expect.hasLength stories.Head.AcceptanceScenarios 1 "Acceptance scenario is extracted."
        }

        test "summarizeFeatureStatus_reports partial artifact states" {
            match (Fixtures.partialProject ()).FeatureRoot with
            | None -> failtest "partial fixture missing feature root"
            | Some root ->
                File.WriteAllText(Path.Combine(root, "tasks.md"), "- [ ] T001 Example")
                let status = SpeckitArtifacts.summarizeFeatureStatus root
                Expect.equal status.SpecState Present "Spec is present."
                Expect.equal status.PlanState Missing "Plan is missing."
                Expect.equal status.TasksState Present "Tasks are present."
        }

        test "summarizeFeatureStatus_reports complete local artifact set" {
            match (Fixtures.completeProject ()).FeatureRoot with
            | None -> failtest "complete fixture missing feature root"
            | Some root ->
                let status = SpeckitArtifacts.summarizeFeatureStatus root
                Expect.equal status.SpecState Present "Spec is present."
                Expect.equal status.PlanState Present "Plan is present."
                Expect.equal status.TasksState Present "Tasks are present."
                Expect.equal status.ChecklistState Present "Checklist is present."
        }

        test "summarizeFeatureStatus_reports malformed artifact content" {
            match (Fixtures.malformedProject ()).FeatureRoot with
            | None -> failtest "malformed fixture missing feature root"
            | Some root ->
                let status = SpeckitArtifacts.summarizeFeatureStatus root
                Expect.isTrue (match status.SpecState with Malformed _ -> true | _ -> false) "Spec is malformed."
                Expect.isTrue (match status.PlanState with Malformed _ -> true | _ -> false) "Plan is malformed."
                Expect.isTrue (match status.TasksState with Malformed _ -> true | _ -> false) "Tasks are malformed."
        }

        test "parseUserStories_reports unreadable file as diagnostic" {
            let path = Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-unreadable-spec-").FullName, "spec.md")
            File.WriteAllText(path, "### User Story 1 - Hidden (Priority: P1)")
            let originalMode = File.GetUnixFileMode path

            try
                File.SetUnixFileMode(path, enum<UnixFileMode> 0)
                let stories, diagnostics = SpeckitArtifacts.parseUserStories path
                Expect.isEmpty stories "Unreadable spec produces no stories."
                Expect.isNonEmpty diagnostics "Unreadable spec is recoverable as a diagnostic."
                Expect.stringContains diagnostics.Head.Message "unreadable" "Underlying read failure is retained."
            finally
                File.SetUnixFileMode(path, originalMode)
        }

        test "parseTasks_reports unreadable file as diagnostic" {
            let path = Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-unreadable-tasks-").FullName, "tasks.md")
            File.WriteAllText(path, "- [ ] T001 Hidden")
            let originalMode = File.GetUnixFileMode path

            try
                File.SetUnixFileMode(path, enum<UnixFileMode> 0)
                let tasks, diagnostics = SpeckitArtifacts.parseTasks path
                Expect.isEmpty tasks "Unreadable tasks produce no parsed tasks."
                Expect.isNonEmpty diagnostics "Unreadable tasks are recoverable as a diagnostic."
                Expect.stringContains diagnostics.Head.Message "unreadable" "Underlying read failure is retained."
            finally
                File.SetUnixFileMode(path, originalMode)
        }

        test "loadPlan_extracts summary and reports missing plan" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-plan-").FullName
            let missing = SpeckitArtifacts.loadPlan root
            Expect.isNonEmpty missing.Diagnostics "Missing plan is diagnostic."

            File.WriteAllText(Path.Combine(root, "plan.md"), "# Plan\n\n## Summary\nPlan summary\n\n## Technical Context\nF#")
            let plan = SpeckitArtifacts.loadPlan root
            Expect.equal plan.Summary (Some "Plan summary") "Summary is extracted."
            Expect.equal plan.TechnicalContext (Some "F#") "Technical context is extracted."
        }

        test "parseTasks_preserves raw status_dependencies_story_and_source" {
            let root = Directory.CreateTempSubdirectory("sk-dashboard-tasks-").FullName
            let path = Path.Combine(root, "tasks.md")
            File.WriteAllText(path, "- [>] T002 [US1] Build after T001\n  Task details\n- [X] T001 Setup")
            let tasks, diagnostics = SpeckitArtifacts.parseTasks path
            Expect.isEmpty diagnostics "Task parsing should succeed."
            let task = tasks |> List.find (fun item -> item.Id = "T002")
            Expect.equal task.RawStatus "[>]" "Raw status is preserved."
            Expect.equal task.Dependencies [ "T001" ] "Dependency is parsed."
            Expect.equal task.RelatedStoryId (Some "US1") "Story relationship is parsed."
            Expect.equal task.Description (Some "Task details") "Description is parsed."
            Expect.equal (task.SourceLocation |> Option.bind _.Line) (Some 1) "Source line is preserved."
        }
    ]
