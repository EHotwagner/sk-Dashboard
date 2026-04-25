module SkDashboard.Core.Tests.TaskGraphTests

open Expecto
open SkDashboard.Core

let task id deps story =
    { Id = id
      Title = id
      Description = None
      RawStatus = "[ ]"
      Dependencies = deps
      RelatedStoryId = story
      SourceLocation = None
      Metadata = Map.empty }

[<Tests>]
let taskGraphTests =
    testList "Task graph" [
        test "build_includes selected story tasks and dependencies" {
            let graph = TaskGraphBuilder.build (Some "US1") [ task "T001" [] None; task "T002" [ "T001" ] (Some "US1") ] []
            Expect.equal (graph.Nodes |> List.map _.Id) [ "T001"; "T002" ] "Dependency chain is included."
            Expect.equal graph.Edges [ { FromTaskId = "T001"; ToTaskId = "T002" } ] "Dependency edge points toward dependent task."
        }

        test "build_includes transitive selected story dependencies" {
            let graph =
                TaskGraphBuilder.build
                    (Some "US1")
                    [ task "T001" [] None
                      task "T002" [ "T001" ] None
                      task "T003" [ "T002" ] (Some "US1") ]
                    []

            Expect.equal (graph.Nodes |> List.map _.Id) [ "T001"; "T002"; "T003" ] "Full transitive chain is included."
            Expect.equal
                graph.Edges
                [ { FromTaskId = "T001"; ToTaskId = "T002" }
                  { FromTaskId = "T002"; ToTaskId = "T003" } ]
                "All dependency-chain edges are retained."
        }

        test "build_reports missing duplicate and cycle diagnostics" {
            let graph =
                TaskGraphBuilder.build
                    (Some "US1")
                    ([ task "T001" [ "T002"; "T404" ] (Some "US1")
                       task "T002" [ "T001" ] None ]
                     @ [ task "T003" [] None
                         task "T003" [] None ])
                    []

            let messages = graph.Diagnostics |> List.map _.Message |> String.concat "\n"
            Expect.stringContains messages "Duplicate task id" "Duplicate ids are reported."
            Expect.stringContains messages "missing dependency T404" "Missing refs are reported."
            Expect.stringContains messages "cycle" "Cycles are reported."
        }
    ]
