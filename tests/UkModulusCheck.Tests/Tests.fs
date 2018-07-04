module UkModulusCheckTests

open Expecto
open UkModulusCheck.Types
open UkModulusCheck.Validator
open System.IO
open System.Reflection

let testCaseM test input =
    input |> Seq.map (fun (sc, an) -> testCase (sprintf "%s-%s" sc an) <| fun _ -> test sc an)

let getTests() =
    let path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

    let rulesTable =
        match loadRules (Path.Combine(path, "valacdos-v490.txt")) with
        | Ok rules -> rules
        | Error ex -> printfn "!!! %A" ex; []

    let substitutionTable =
        match loadSubstitutions (Path.Combine(path, "scsubtab.txt")) with
        | Ok subs -> subs
        | Error ex -> printfn "!!! %A" ex; []

    let expectToBeValid sc an =
        let result = validateAccountNo rulesTable substitutionTable sc an
        Expect.equal result Valid (sprintf "This should be a valid account: %s-%s" sc an)

    let expectToBeInvalid reason sc an =
        let result = validateAccountNo rulesTable substitutionTable sc an
        Expect.equal result (Invalid reason) (sprintf "This shouldn't be a valid account: %s-%s" sc an)

    testList "Sort Code and Account Number validation checks" [
        testList "Verify data is loaded" [
            testCase "Rules are loaded." <| fun _ ->
                Expect.isNonEmpty rulesTable (sprintf "Couldn't load rules table from path: %s" path)

            testCase "Substitutions are loaded." <| fun _ ->
                Expect.isNonEmpty substitutionTable (sprintf "Couldn't load substitutions table from path: %s" path)
        ]

        testList "Non-verifiable cases" [
            testCase "Sort code not in rules table." <| fun _ ->
                expectToBeValid "001122" "44556677"
        ]

        testList "No exceptions cases" [
            testList "Pass modulus 10 check." [
                yield! ["089999","66374958"; "090128","03745521"]
                |> testCaseM expectToBeValid
            ]

            testList "Pass modulus 11 check." [
                yield! ["107999", "88837491"; "560003","13354647"]
                |> testCaseM expectToBeValid
            ]

            testList "Pass double alternate check." [
                yield! ["040469","00000312"; "040469","00001010"]
                |> testCaseM expectToBeValid
            ]

            testList "Pass modulus 11 and double alternate checks." [
                yield! ["202959", "63748472"; "404784", "70872490"]
                |> testCaseM expectToBeValid
            ]

            testCase "Pass modulus 11 check and fail double alternate check." <| fun _ ->
                expectToBeInvalid ModulusCheckFailed "203099" "66831036"

            testCase "Fail modulus 11 check and pass double alternate check." <| fun _ ->
                expectToBeInvalid ModulusCheckFailed "203099" "58716970"

            testCase "Fail modulus 11 check and skip double alternate check." <| fun _ ->
                expectToBeInvalid ModulusCheckFailed "404784" "70872491"

            testList "Fail modulus 10 check." [
                yield! ["089999","66374959"; "090128","13745521"]
                |> testCaseM (expectToBeInvalid ModulusCheckFailed)
            ]

            testList "Fail modulus 11 check." [
                yield! ["107999","88837493"; "560003","23354647"]
                |> testCaseM (expectToBeInvalid ModulusCheckFailed)
            ]

            testList "Fail double alternate check." [
                yield! ["040469","00000311"; "040469","00001011"]
                |> testCaseM (expectToBeInvalid ModulusCheckFailed)
            ]
        ]

        testList "Exception 1 cases" [
            testCase "Exception 1 – ensures that 27 has been added to the accumulated total and passes double alternate modulus check." <| fun _ ->
                expectToBeValid "118765" "64371389"

            testCase "Exception 1 where it fails double alternate check." <| fun _ ->
                expectToBeInvalid ModulusCheckFailed "118765" "64371388"
        ]

        testList "Exception 2 and 9 cases" [
            testCase "Exception 2 & 9 where the first check passes." <| fun _ ->
                expectToBeValid "309070" "02355688"

            testList "Exception 2 & 9 where the first check fails and second check passes with substitution." [
                yield! ["309070","12345668"; "308088","14457846"]
                |> testCaseM expectToBeValid
            ]

            testCase "Exception 2 & 9 where a≠0 and g≠9 and passes." <| fun _ ->
                expectToBeValid "309070" "12345677"

            testCase "Exception 2 & 9 where a≠0 and g=9 and passes." <| fun _ ->
                expectToBeValid "309070" "99345694"

            testList "Exception 2 & 9 where both checks fail." [
                yield! ["308087","25337846"; "308088","24457846"]
                |> testCaseM (expectToBeInvalid ModulusCheckFailed)
            ]
        ]

        testList "Exception 3 cases" [
            testCase "Exception 3, and the sorting code is the start of a range. As c=6 the second check should be ignored." <| fun _ ->
                expectToBeValid "820000" "73688637"

            testCase "Exception 3, and the sorting code is the end of a range. As c=9 the second check should be ignored." <| fun _ ->
                expectToBeValid "827999" "73988638"

            testCase "Exception 3. As c<>6 or 9 perform both checks pass." <| fun _ ->
                expectToBeValid "827101" "28748352"
        ]

        testList "Exception 4 cases" [
            testCase "Exception 4 where the remainder is equal to the checkdigit." <| fun _ ->
                expectToBeValid "134020" "63849203"
        ]

        testList "Exception 5 cases" [
            testCase "Exception 5 where the check passes." <| fun _ ->
                expectToBeValid "938611" "07806039"

            testCase "Exception 5 where the check passes with substitution." <| fun _ ->
                expectToBeValid "938600" "42368003"

            testCase "Exception 5 where both checks produce a remainder of 0 and pass." <| fun _ ->
                expectToBeValid "938063" "55065200"

            testCase "Exception 5 where the first checkdigit is correct and the second incorrect." <| fun _ ->
                expectToBeInvalid ModulusCheckFailed "938063" "15764273"

            testCase "Exception 5 where the first checkdigit is incorrect and the second correct." <| fun _ ->
                expectToBeInvalid ModulusCheckFailed "938063" "15764264"

            testCase "Exception 5 where the first checkdigit is incorrect with a remainder of 1." <| fun _ ->
                expectToBeInvalid ModulusCheckFailed "938063" "15763217"
        ]

        testList "Exception 6 cases" [
            testCase "Exception 6 where both checks pass." <| fun _ ->
                expectToBeValid "205132" "13537846"

            testCase "Exception 6 where no modulus check is performed." <| fun _ ->
                expectToBeValid "205132" "43537844"

            testCase "Exception 6 where the account fails standard check but is a foreign currency account." <| fun _ ->
                expectToBeValid "200915" "41011166"

            testCase "Exception 6 where check fails." <| fun _ ->
                expectToBeInvalid ModulusCheckFailed "205132" "23537846"
        ]

        testList "Exception 7 cases" [
            testCase "Exception 7 where passes but would fail the standard check." <| fun _ ->
                expectToBeValid "772798" "99345694"
        ]

        testList "Exception 8 cases" [
            testCase "Exception 8 where the check passes." <| fun _ ->
                expectToBeValid "086090" "06774744"
        ]

        testList "Exception 10 and 11 cases" [
            testCase "Exception 10 & 11 where first check passes and second check fails." <| fun _ ->
                expectToBeValid "871427" "46238510"

            testCase "Exception 10 & 11 where first check fails and second check passes." <| fun _ ->
                expectToBeValid "872427" "46238510"

            testCase "Exception 10 where in the account number ab=09 and the g=9. The first check passes and second check fails." <| fun _ ->
                expectToBeValid "871427" "09123496"

            testCase "Exception 10 where in the account number ab=99 and the g=9. The first check passes and the second check fails." <| fun _ ->
                expectToBeValid "871427" "99123496"
        ]

        testList "Exception 12 and 13 cases" [
            testCase "Exception 12/13 where passes modulus 11 check (in this example, modulus 10 check fails, however, there is no need for it to be performed as the first check passed)." <| fun _ ->
                expectToBeValid "074456" "12345112"

            testCase "Exception 12/13 where passes the modulus 11check (in this example, modulus 10 check passes as well, however, there is no need for it to be performed as the first check passed)." <| fun _ ->
                expectToBeValid "070116" "34012583"

            testCase "Exception 12/13 where fails the modulus 11 check, but passes the modulus 10 check." <| fun _ ->
                expectToBeValid "074456" "11104102"
        ]

        testList "Exception 14 cases" [
            testCase "Exception 14 where the first check fails and the second check passes." <| fun _ ->
                expectToBeValid "180002" "00000190"
        ]

        testList "Non-standard length of account number" [
            testCase "Account number has 6 digits." <| fun _ ->
                expectToBeValid "180002" "000190"

            testCase "Account number has 7 digits." <| fun _ ->
                expectToBeValid "086090" "6774744"

            testCase "Account number has 9 digits." <| fun _ ->
                expectToBeValid "089990" "966374958"

            testCase "Account number has 10 digits (Co-Operative Bank plc)." <| fun _ ->
                expectToBeValid "089999" "6637495842"

            testCase "Account number has 10 digits (other cases)." <| fun _ ->
                expectToBeValid "107999" "4288837491"

            testCase "Account number has whitespace." <| fun _ ->
                expectToBeValid "000000" "   12345678 "
        ]

        testList "Invalid account number" [
            testCase "Account number has non-digit characters." <| fun _ ->
                expectToBeInvalid AccountNumberInvalidFormat "000000" "abcdefgh"

            testCase "Account number has too many digits." <| fun _ ->
                expectToBeInvalid AccountNumberInvalidLength "000000" "12345678901"

            testCase "Account number has not enough digits." <| fun _ ->
                expectToBeInvalid AccountNumberInvalidLength "000000" "12345"
        ]

        testList "Sort code with separators or whitespace" [
            testCase "Sort code has dash separators." <| fun _ ->
                expectToBeValid "00-00-00" "12345678"

            testCase "Sort code has whitespace separators." <| fun _ ->
                expectToBeValid "00 00 00" "12345678"

            testCase "Sort code has whitespace." <| fun _ ->
                expectToBeValid " 000000  " "12345678"
        ]

        testList "Invalid sort code" [
            testCase "Sort code has invalid characters." <| fun _ ->
                expectToBeInvalid SortCodeInvalidFormat "00*00*00" "12345678"

            testCase "Sort code has separators in wrong places." <| fun _ ->
                expectToBeInvalid SortCodeInvalidFormat "000-000" "12345678"

            testCase "Sort code has too many digits." <| fun _ ->
                expectToBeInvalid SortCodeInvalidLength "00-00-000" "12345678"

            testCase "Sort code has not enough digits." <| fun _ ->
                expectToBeInvalid SortCodeInvalidLength "00000" "12345678"
        ]
    ]

[<Tests>]
let tests = getTests()
