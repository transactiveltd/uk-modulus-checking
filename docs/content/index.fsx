(*** hide ***)
#r "../../src/UkModulusCheck/bin/Release/net461/UkModulusCheck.dll"

(**
# UK Modulus Check

This library provides the functions to validate UK sort code and account number
according to the specification provided by [VocaLink](https://www.vocalink.com/customer-support/modulus-checking/).

The library is implemented in F# and can be used from any .NET project.
Library is available for both the full framework projects (net461) and the .NET Core/Standard projects (netstandard2.0).

## Usage

To check the sort code / account number pair you'll need:

* [Rules and weightings table](https://www.vocalink.com/media/3003/valacdos-v490.txt)
* [Sort code substitution table](https://www.vocalink.com/media/1517/scsubtab.txt).

You can obtain newest version of both files from [VocaLink](https://www.vocalink.com/customer-support/modulus-checking/).

Once you got the files, you can load the data into memory (and potentially cache it) using provided functions:
*)

open UkModulusCheck.Validator

let rules =
    match loadRules "valacdos-v490.txt" with
    | Ok r -> r
    | Error ex -> [] // Handle loading error
let substitutions =
    match loadSubstitutions "scsubtab.txt" with
    | Ok s -> s
    | Error ex -> [] // Handle loading error

(**
You can find the details of the functions in [the reference docs](reference/ukmoduluscheck-validator.html).

To validate the sort code and account number pair use `validateAccountNo` function:
*)

match validateAccountNo rules substitutions sortCode accountNumber with
| Valid -> printfn "%s-%s is valid!" sortCode accountNumber
| Invalid -> printfn "%s-%s is invalid!" sortCode accountNumber

(**
For more information about the API and types used by the library please check [the reference docs](reference/index.html).
*)
