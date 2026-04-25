#r "../../../src/Core/bin/Debug/net10.0/Core.dll"

open System.IO
open SkDashboard.Core

let dir = Directory.CreateTempSubdirectory("sk-dashboard-fsi-")
let path = Path.Combine(dir.FullName, "dashboard.json")

File.WriteAllText(
    path,
    """{"version":1,"bindings":[{"command":"story.next","key":"n"}],"ui":{"layout":"vertical","colors":{"panelAccent":"#7aa2f7"}}}""")

let preferences = Hotkeys.loadPreferences path

printfn "bindings=%d" preferences.Bindings.Length
printfn "story.next=%s" ((preferences.Bindings |> List.find (fun binding -> binding.Command = StoryNext)).KeySequence)
printfn "layout=%A" preferences.Ui.Layout
printfn "panelAccent=%s" preferences.Ui.Colors[PanelAccent].Foreground
printfn "diagnostics=%d" preferences.Diagnostics.Length
