module sk_Dashboard.Tests

open Expecto
open sk_Dashboard

// Principle I reminder: semantic tests exercise the library through the
// same surface a script or FSI session would use — the module's public
// functions declared in Library.fsi. Do not reach for internals.

[<Tests>]
let libraryTests =
    testList "Library" [
        test "add_returns sum of two positives" {
            let result = Library.add 2 3
            Expect.equal result 5 "2 + 3 = 5"
        }

        test "add_is commutative" {
            Expect.equal (Library.add 2 3) (Library.add 3 2) "a+b = b+a"
        }
    ]
