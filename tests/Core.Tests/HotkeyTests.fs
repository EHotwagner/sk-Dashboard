module SkDashboard.Core.Tests.HotkeyTests

open System.IO
open Expecto
open SkDashboard.Core

[<Tests>]
let hotkeyTests =
    testList "Hotkeys" [
        test "defaultBindings_cover primary commands" {
            let commands =
                Hotkeys.defaultBindings
                |> List.map _.Command
                |> Set.ofList

            Expect.equal commands.Count 14 "Every primary command has a default binding."
            Expect.equal (Hotkeys.defaultBindings |> List.find (fun binding -> binding.Command = FeaturePrevious)).KeySequence "k" "Feature previous defaults to k."
            Expect.equal (Hotkeys.defaultBindings |> List.find (fun binding -> binding.Command = FeatureNext)).KeySequence "j" "Feature next defaults to j."
            Expect.equal (Hotkeys.defaultBindings |> List.find (fun binding -> binding.Command = StoryPrevious)).KeySequence "up" "Story previous defaults to up arrow."
            Expect.equal (Hotkeys.defaultBindings |> List.find (fun binding -> binding.Command = StoryNext)).KeySequence "down" "Story next defaults to down arrow."
            Expect.equal (Hotkeys.defaultBindings |> List.find (fun binding -> binding.Command = TaskPrevious)).KeySequence "left" "Task previous defaults to left arrow."
            Expect.equal (Hotkeys.defaultBindings |> List.find (fun binding -> binding.Command = TaskNext)).KeySequence "right" "Task next defaults to right arrow."
        }

        test "validateBindings_reports duplicate keys" {
            let bindings =
                [ { Command = Refresh; KeySequence = "r"; Scope = "dashboard"; Source = "test" }
                  { Command = Quit; KeySequence = "r"; Scope = "dashboard"; Source = "test" } ]

            Expect.isNonEmpty (Hotkeys.validateBindings bindings) "Duplicate key is a conflict."
        }

        test "loadBindings_applies valid global override" {
            let path = Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-hotkeys-").FullName, "hotkeys.json")
            File.WriteAllText(path, """{"version":1,"bindings":[{"command":"story.next","key":"n"}]}""")
            let bindings, diagnostics = Hotkeys.loadBindings path
            Expect.isEmpty diagnostics "Valid config has no diagnostics."
            let storyNext = bindings |> List.find (fun binding -> binding.Command = StoryNext)
            Expect.equal storyNext.KeySequence "n" "User override is active."
        }

        test "loadBindings_reports unsupported_unknown_and_conflicting_bindings" {
            let path = Path.Combine(Directory.CreateTempSubdirectory("sk-dashboard-hotkeys-bad-").FullName, "hotkeys.json")
            File.WriteAllText(path, """{"version":1,"bindings":[{"command":"story.next","key":"bad key"},{"command":"refresh","key":"q"},{"command":"quit","key":"q"},{"command":"unknown","key":"u"}]}""")
            let bindings, diagnostics = Hotkeys.loadBindings path
            Expect.equal bindings Hotkeys.defaultBindings "Invalid config falls back to defaults."
            let messages = diagnostics |> List.map _.Message |> String.concat "\n"
            Expect.stringContains messages "Unsupported hotkey sequence" "Unsupported keys are reported."
            Expect.stringContains messages "Conflicting hotkey binding" "Conflicts are reported."
            Expect.stringContains messages "Unknown hotkey command" "Unknown commands are reported."
        }
    ]
