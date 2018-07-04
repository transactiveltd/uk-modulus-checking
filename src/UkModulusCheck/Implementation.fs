namespace UkModulusCheck

module internal Implementation =
    open System
    open Helpers
    open Types

    module Position =
        let U, V, W, X, Y, Z, A, B, C, D, E,  F,  G,  H =
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13

    let parseMethod = function
        | "MOD10" -> Some Standard10
        | "MOD11" -> Some Standard11
        | "DBLAL" -> Some DoubleAlternate
        | _ -> None

    let parseValidationRule (line: string) =
        match line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) with
        | xs when xs.Length = 18 ->
            let scFrom, scTo = xs.[0], xs.[1]
            let method = xs.[2] |> parseMethod
            let weightings = xs.[3..16] |> Array.toList |> List.map str2int |> List.sequence
            let ex = str2int xs.[17]
            Option.map3 (fun m ws e ->
                { SortCodeFrom = scFrom; SortCodeTo = scTo; Method = m; Weightings = ws; Exception = Some (Exception e) })
                method weightings ex
        | xs when xs.Length = 17 ->
            let scFrom, scTo = xs.[0], xs.[1]
            let method = xs.[2] |> parseMethod
            let weightings = xs.[3..16] |> Array.toList |> List.map str2int |> List.sequence
            Option.map2 (fun m ws ->
                { SortCodeFrom = scFrom; SortCodeTo = scTo; Method = m; Weightings = ws; Exception = None })
                method weightings
        | _ -> None

    let parseSubstitution (line: string) =
        match line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) with
        | [|scFrom; scTo|] -> Some { SortCode = scFrom; SubstituteWith = scTo }
        | _ -> None

    let validateSortCode (sortCode: SortCode) =
        match sortCode with
        | null -> Invalid FailureReason.SortCodeInvalidLength
        | Trimmed sc when sc.Length < 6 || sc.Length > 8 -> Invalid FailureReason.SortCodeInvalidLength
        | MatchesTrimmed "^\d{2}[-\s]?\d{2}[-\s]?\d{2}$" true -> Valid
        | _ -> Invalid FailureReason.SortCodeInvalidFormat

    let validateAccountNo (accountNo: AccountNumber) =
        match accountNo with
        | null -> Invalid FailureReason.AccountNumberInvalidLength
        | Trimmed an when an.Length < 6 || an.Length > 10 -> Invalid FailureReason.AccountNumberInvalidLength
        | MatchesTrimmed "^\d{6,10}$" true -> Valid
        | _ -> Invalid FailureReason.AccountNumberInvalidFormat

    let standardise (sortCode: string) (accountNo: string) =
        match sortCode.Length, accountNo.Length with
        | 6, 6 -> sortCode, "00" + accountNo
        | 6, 7 -> sortCode, "0" + accountNo
        | 6, 9 -> sortCode.[0..4] + accountNo.[0..0], accountNo.[1..] // For Santander
        | 6, 10 ->
            if sortCode.StartsWith("08") // Co-Operative Bank plc
            then sortCode, accountNo.[..7]
            else sortCode, accountNo.[2..]
        | _ -> sortCode, accountNo

    let doubleAlternateModulus (weightings: Weight list) (number: string) : int =
        number
        |> Seq.map char2int
        |> Seq.map2 (*) weightings
        |> Seq.collect (Seq.unfold (fun x -> if x > 0 then Some (x % 10, x / 10) else None))
        |> Seq.sum

    let standardModulus (weightings: Weight list) (number: string) : int =
        number
        |> Seq.map char2int
        |> Seq.map2 (*) weightings
        |> Seq.sum

    let invalidIfFalse result = if result then Valid else Invalid FailureReason.ModulusCheckFailed

    let validateRule rule number =
        match rule.Method with
        | Standard10 -> standardModulus rule.Weightings number % 10 = 0
        | Standard11 -> standardModulus rule.Weightings number % 11 = 0
        | DoubleAlternate -> doubleAlternateModulus rule.Weightings number % 10 = 0

    let validateRules (rules: ValidationRule list) (substitutionTable: SortCodeSubstitution list) (sortCode: SortCode) (accountNo: AccountNumber) =
        let number = sortCode + accountNo
        let exceptions = rules |> List.map (fun r -> r.Exception)
        match rules, exceptions with
        | [], _ -> Valid

        | [rule], [None] ->
            validateRule rule number |> invalidIfFalse

        | [rule1; rule2], [None; None] ->
            (validateRule rule1 number && validateRule rule2 number) |> invalidIfFalse

        | [rule], [Some (Exception 1)] ->
            // Perform the double alternate check except: Add 27 to the total (ie before you divide by 10)
            (doubleAlternateModulus rule.Weightings number + 27) % 10 = 0  |> invalidIfFalse

        | [rule1; rule2], [Some (Exception 2); Some (Exception 9)] ->
            // Only occurs for some standard modulus 11 checks, when there is a 2 in the exception column for the first check for a sorting code
            // and a 9 in the exception column for the second check for the same sorting code. This is used specifically for Lloyds euro accounts.
            let firstCheck =
                let weightings =
                    match number.[Position.A], number.[Position.G] with
                    | Not '0' _, Not '9' _ -> [0;0;1;2;5;3;6;4;8;7;10;9;3;1]
                    | Not '0' _, '9' -> [0;0;0;0;0;0;0;0;8;7;10;9;3;1]
                    | _ -> rule1.Weightings
                standardModulus weightings number % 11 = 0

            let secondCheck =
                // All Lloyds euro accounts are held at sorting code 30-96-34, however customers may perceive that their euro account
                // is held at the branch where sterling accounts are held and thus quote a sorting code other than 30-96-34.
                // The combination of the “sterling” sorting code and “euro” account number will cause the first standard modulus 11 check to fail.
                // In such cases, carry out the second modulus 11 check, substituting the sorting code with 309634 and the appropriate weighting.
                // If this check passes it is deemed to be a valid euro account.
                let number' = "309634" + accountNo
                standardModulus rule2.Weightings number' % 11 = 0

            // If the first row with exception 2 passes the standard modulus 11 check, you do not need to carry out the second check (ie it is deemed to be a valid sterling account).
            (firstCheck || secondCheck) |> invalidIfFalse

        | [rule1; rule2], [None; Some (Exception 3)] ->
            let secondCheck =
                // If c=6 or c=9 the double alternate check does not need to be carried out.
                match number.[Position.C] with
                | '6' | '9' -> true
                | _ -> validateRule rule2 number

            (validateRule rule1 number || secondCheck) |> invalidIfFalse

        | [rule], [Some (Exception 4)] ->
            // Perform the standard modulus 11 check.
            // After you have finished the check, ensure that the remainder is the same as the two-digit checkdigit;
            // the checkdigit for exception 4 is gh from the original account number.
            let modulus = standardModulus rule.Weightings number % 11
            let checkdigit = (char2int number.[Position.G]) * 10 + (char2int number.[Position.H])
            modulus = checkdigit |> invalidIfFalse

        | [rule1; rule2], [Some (Exception 5); Some (Exception 5)] ->
            // Perform the first check (standard modulus check) except:
            // If the sorting code appears in this table in the “Original s/c” column, substitute it for the “substitute with” column (for check purposes only).
            // If the sorting code is not found, use the original sorting code.
            let sortCode' =
                match substitutionTable |> List.tryFind (fun sub -> sub.SortCode = sortCode) with
                | Some sub -> sub.SubstituteWith
                | None -> sortCode
            let number' = sortCode' + accountNo

            let firstCheck =
                // For the standard check with exception 5 the checkdigit is g from the original account number.
                // After dividing the result by 11:
                // - if the remainder = 0 and g = 0 the account number is valid
                // - if the remainder = 1 the account number is invalid
                // - for all other remainders, take the remainder away from 11. If the number you get is the same as g then the account number is valid.
                match standardModulus rule1.Weightings number' % 11 with
                | 0 when number'.[Position.G] = '0' -> true
                | 1 -> false
                | reminder -> (11 - reminder) = char2int number'.[Position.G]

            let secondCheck =
                // Perform the second double alternate check, and for the double alternate check with exception 5 the checkdigit is h from the original account number, except:
                // After dividing the result by 10:
                // - if the remainder = 0 and h = 0 the account number is valid
                // - for all other remainders, take the remainder away from 10. If the number you get is the same as h then the account number is valid.
                match doubleAlternateModulus rule2.Weightings number' % 10 with
                | 0 when number'.[Position.H] = '0' -> true
                | reminder -> (10 - reminder) = char2int number'.[Position.H]

            (firstCheck && secondCheck) |> invalidIfFalse

        | [rule1; rule2], [Some (Exception 6); Some (Exception 6)] ->
            // Indicates that these sorting codes may contain foreign currency accounts which cannot be checked. Perform the first and second checks, except:
            // If a = 4, 5, 6, 7 or 8, and g and h are the same, the accounts are for a foreign currency and the checks cannot be used.
            if ("45678" |> Seq.contains number.[Position.A]) && number.[Position.G] = number.[Position.H]
            then Valid
            else (validateRule rule1 number && validateRule rule2 number) |> invalidIfFalse

        | [rule], [Some (Exception 7)] ->
            // Perform the check as specified, except if g = 9 zeroise weighting positions u-b.
            let rule', number' =
                if number.[Position.G] = '9'
                then { rule with Weightings = rule.Weightings.[Position.B+1..] }, number.[Position.B+1..]
                else rule, number
            validateRule rule' number' |> invalidIfFalse

        | [rule], [Some (Exception 8)] ->
            // Perform the check as specified, except substitute the sorting code with 090126, for check purposes only.
            validateRule rule ("090126" + number.[Position.A..]) |> invalidIfFalse

        | [rule1; rule2], [Some (Exception 10); Some (Exception 11)] ->
            // These exceptions are for some Lloyds accounts and some TSB accounts. If there is a 10 in the exception column for the first check for a sorting code
            // and an 11 in the exception column for the second check for the same sorting code, if either check is successful the account number is deemed valid.

            let firstCheck =
                // For the exception 10 check, if ab = 09 or ab = 99 and g = 9, zeroise weighting positions u-b.
                let rule1' =
                    if ["09"; "99"] |> List.contains number.[Position.A..Position.B] && number.[Position.G] = '9'
                    then { rule1 with Weightings = [for i, w in rule1.Weightings |> List.indexed -> if i >= Position.U && i <= Position.B then 0 else w] }
                    else rule1
                validateRule rule1' number

            (firstCheck || validateRule rule2 number) |> invalidIfFalse

        | [rule1; rule2], [Some (Exception 12); Some (Exception 13)] ->
            // Where there is a 12 in the exception column for the first check for a sorting code and a 13 in the exception column for the second check for the same sorting code,
            // if either check is successful the account number is deemed valid.
            (validateRule rule1 number || validateRule rule2 number) |> invalidIfFalse

        | [rule], [Some (Exception 14)] ->
            // Perform the modulus 11 check as normal: If the check passes (that is, there is no remainder), then the account number should be considered valid. Do not perform the second check.
            // If the first check fails, then the second check must be performed as specified below.
            let secondCheck =
                // If the 8th digit of the account number (reading from left to right) is not 0, 1 or 9 then the account number fails the second check and is not a valid Coutts account number.
                // If the 8th digit is 0, 1 or 9, then remove the digit from the account number and insert a 0 as the 1st digit for check purposes only.
                // Perform the modulus 11 check on the modified account number using the same weightings as specified in the table.
                // - If there is no remainder, then the account number should be considered valid.
                // - If there is a remainder, then the account number fails the second check and is not a valid Coutts account number
                if "019" |> Seq.contains accountNo.[7]
                then validateRule rule (sortCode + "0" + accountNo.[..6])
                else false

            (validateRule rule number || secondCheck) |> invalidIfFalse

        | _ -> Invalid FailureReason.UnrecognizedRule
