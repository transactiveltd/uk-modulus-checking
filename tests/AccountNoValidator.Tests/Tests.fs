module AccountNoValidatorTests

open Expecto
open AccountNoValidator.Types
open AccountNoValidator.Validator

let rulesTable = []
let substitutionTable = []

[<Tests>]
let tests =
    testList "Sort Code and Account Number validation checks" [
        testList "No exceptions cases" [
            testCase "Pass modulus 10 check." <| fun _ ->
                let sc, an = "089999", "66374958"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Pass modulus 11 check." <| fun _ ->
                let sc, an = "107999", "88837491"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Pass modulus 11 and double alternate checks." <| fun _ ->
                let sc, an = "202959", "63748472"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Pass modulus 11 check and fail double alternate check." <| fun _ ->
                let sc, an = "203099", "66831036"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Invalid "This shouldn't be a valid account"

            testCase "Fail modulus 11 check and pass double alternate check." <| fun _ ->
                let sc, an = "203099", "58716970"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Invalid "This shouldn't be a valid account"

            testCase "Fail modulus 10 check." <| fun _ ->
                let sc, an = "089999", "66374959"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Invalid "This shouldn't be a valid account"

            testCase "Fail modulus 11 check." <| fun _ ->
                let sc, an = "107999", "88837493"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Invalid "This shouldn't be a valid account"
        ]

        testList "Exception 1 cases" [
            testCase "Exception 1 – ensures that 27 has been added to the accumulated total and passes double alternate modulus check." <| fun _ ->
                let sc, an = "118765", "64371389"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 1 where it fails double alternate check." <| fun _ ->
                let sc, an = "118765", "64371388"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Invalid "This shouldn't be a valid account"
        ]

        testList "Exception 2 and 9 cases" [
            testCase "Exception 2 & 9 where the first check passes." <| fun _ ->
                let sc, an = "309070", "02355688"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 2 & 9 where the first check fails and second check passes with substitution." <| fun _ ->
                let sc, an = "309070", "12345668"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 2 & 9 where a≠0 and g≠9 and passes." <| fun _ ->
                let sc, an = "309070", "12345677"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 2 & 9 where a≠0 and g=9 and passes." <| fun _ ->
                let sc, an = "309070", "99345694"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]

        testList "Exception 3 cases" [
            testCase "Exception 3, and the sorting code is the start of a range. As c=6 the second check should be ignored." <| fun _ ->
                let sc, an = "820000", "73688637"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 3, and the sorting code is the end of a range. As c=9 the second check should be ignored." <| fun _ ->
                let sc, an = "827999", "73988638"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 3. As c<>6 or 9 perform both checks pass." <| fun _ ->
                let sc, an = "827101", "28748352"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]

        testList "Exception 4 cases" [
            testCase "Exception 4 where the remainder is equal to the checkdigit." <| fun _ ->
                let sc, an = "134020", "63849203"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]

        testList "Exception 5 cases" [
            testCase "Exception 5 where the check passes." <| fun _ ->
                let sc, an = "938611", "07806039"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 5 where the check passes with substitution." <| fun _ ->
                let sc, an = "938600", "42368003"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 5 where both checks produce a remainder of 0 and pass." <| fun _ ->
                let sc, an = "938063", "55065200"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 5 where the first checkdigit is correct and the second incorrect." <| fun _ ->
                let sc, an = "938063", "15764273"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Invalid "This shouldn't be a valid account"

            testCase "Exception 5 where the first checkdigit is incorrect and the second correct." <| fun _ ->
                let sc, an = "938063", "15764264"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Invalid "This shouldn't be a valid account"

            testCase "Exception 5 where the first checkdigit is incorrect with a remainder of 1." <| fun _ ->
                let sc, an = "938063", "15763217"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Invalid "This shouldn't be a valid account"
        ]

        testList "Exception 6 cases" [
            testCase "Exception 6 where the account fails standard check but is a foreign currency account." <| fun _ ->
                let sc, an = "200915", "41011166"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]

        testList "Exception 7 cases" [
            testCase "Exception 7 where passes but would fail the standard check." <| fun _ ->
                let sc, an = "772798", "99345694"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]

        testList "Exception 8 cases" [
            testCase "Exception 8 where the check passes." <| fun _ ->
                let sc, an = "086090", "06774744"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]

        testList "Exception 10 and 11 cases" [
            testCase "Exception 10 & 11 where first check passes and second check fails." <| fun _ ->
                let sc, an = "871427", "46238510"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 10 & 11 where first check fails and second check passes." <| fun _ ->
                let sc, an = "872427", "46238510"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 10 where in the account number ab=09 and the g=9. The first check passes and second check fails." <| fun _ ->
                let sc, an = "871427", "09123496"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 10 where in the account number ab=99 and the g=9. The first check passes and the second check fails." <| fun _ ->
                let sc, an = "871427", "99123496"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]

        testList "Exception 12 and 13 cases" [
            testCase "Exception 12/13 where passes modulus 11 check (in this example, modulus 10 check fails, however, there is no need for it to be performed as the first check passed)." <| fun _ ->
                let sc, an = "074456", "12345112"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 12/13 where passes the modulus 11check (in this example, modulus 10 check passes as well, however, there is no need for it to be performed as the first check passed)." <| fun _ ->
                let sc, an = "070116", "34012583"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"

            testCase "Exception 12/13 where fails the modulus 11 check, but passes the modulus 10 check." <| fun _ ->
                let sc, an = "074456", "11104102"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]

        testList "Exception 14 cases" [
            testCase "Exception 14 where the first check fails and the second check passes." <| fun _ ->
                let sc, an = "180002", "00000190"
                let result = validateAccountNo rulesTable substitutionTable sc an
                Expect.equal result Valid "This should be a valid account"
        ]
    ]