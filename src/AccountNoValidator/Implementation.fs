namespace AccountNoValidator

open System.Net
module internal Implementation =
    open System
    open Types

    module Position =
        let U: Weight = 0
        let V: Weight = 1
        let W: Weight = 2
        let X: Weight = 3
        let Y: Weight = 4
        let Z: Weight = 5
        let A: Weight = 6
        let B: Weight = 7
        let C: Weight = 8
        let D: Weight = 9
        let E: Weight = 10
        let F: Weight = 11
        let G: Weight = 12
        let H: Weight = 13

    let (|Not|_|) c input =
        if input <> c then Some c else None

    let char2int = Char.GetNumericValue >> int

    let isDigitString s = s |> Seq.exists (Char.IsDigit >> not) |> not

    let standardise (sortCode: string) (accountNo: string) =
        match sortCode.Length, accountNo.Length with
        | 6, 8 -> Some (sortCode, accountNo)
        | 6, 6 -> Some (sortCode, "00" + accountNo)
        | 6, 7 -> Some (sortCode, "0" + accountNo)
        | 6, 9 -> Some (sortCode.[0..4] + accountNo.[0..0], accountNo.[1..]) //TODO check SC if it's Santander?
        | 6, 10 ->
            if sortCode.StartsWith("08") // Co-Operative Bank plc
            then Some (sortCode, accountNo.[..7])
            else Some (sortCode, accountNo.[2..])
        | _ -> None

    let doubleAlternate (weights: Weight list) (number: string) : int =
        number
        |> Seq.map char2int
        |> Seq.map2 (*) weights
        |> Seq.collect (Seq.unfold (fun x -> if x > 0 then Some (x % 10, x / 10) else None))
        |> Seq.sum

    let standard (weights: Weight list) (number: string) : int =
        number
        |> Seq.map char2int
        |> Seq.map2 (*) weights
        |> Seq.sum

    // doubleAlternate [2;1;2;1;2;1;2;1;2;1;2;1;2;1] "49927312345678"
    // standard 11 [0;0;0;0;0;0;7;5;8;3;4;6;2;1] "00000058177632"

    let validate rule number =
        match rule.Method with
        | Standard10 -> standard rule.Weights number % 10 = 0
        | Standard11 -> standard rule.Weights number % 11 = 0
        | DoubleAlternate -> doubleAlternate rule.Weights number % 10 = 0

    let validateRules (rules: ValidationRule list) (substitutionTable: SortCodeSubstitution list) (sortCode: SortCode) (accountNo: AccountNumber) =
        let number = sortCode + accountNo
        let exceptions = rules |> List.choose (fun r -> r.Exception)
        let isValid =
            match rules, exceptions with
            | [rule], [] ->
                validate rule number

            | [rule1; rule2], [] ->
                (validate rule1 number) && (validate rule2 number)

            | [rule], [ex] ->
                match ex with
                | Exception 1 ->
                    // Perform the double alternate check except: Add 27 to the total (ie before you divide by 10)
                    (doubleAlternate rule.Weights number + 27) % 10 = 0

                | Exception 3 ->
                    // If c=6 or c=9 the double alternate check does not need to be carried out.
                    match number.[Position.C] with
                    | '6' | '9' -> true
                    | _ -> validate rule number

                | Exception 4 ->
                    // Perform the standard modulus 11 check.
                    // After you have finished the check, ensure that the remainder is the same as the two-digit checkdigit;
                    // the checkdigit for exception 4 is gh from the original account number.
                    let modulus = standard rule.Weights number % 11
                    let checkdigit = (char2int number.[Position.G]) * 10 + (char2int number.[Position.H])
                    modulus = checkdigit

                | Exception 7 ->
                    // Perform the check as specified, except if g = 9 zeroise weighting positions u-b.
                    let rule', number' =
                        if number.[Position.G] = '9'
                        then { rule with Weights = rule.Weights.[Position.B+1..] }, number.[Position.B+1..]
                        else rule, number
                    validate rule' number'

                | Exception 8 ->
                    // Perform the check as specified, except substitute the sorting code with 090126, for check purposes only.
                    validate rule ("090126" + number.[6..])

                | _ -> true

            | [rule1; rule2], [ex1; ex2] ->
                match ex1, ex2 with
                | Exception 2, Exception 9 ->
                    // Only occurs for some standard modulus 11 checks, when there is a 2 in the exception column for the first check for a sorting code
                    // and a 9 in the exception column for the second check for the same sorting code. This is used specifically for Lloyds euro accounts.
                    let weights =
                        match number.[Position.A], number.[Position.G] with
                        | Not '0' _, Not '9' _ -> [0;0;1;2;5;3;6;4;8;7;10;9;3;1]
                        | Not '0' _, '9' -> [0;0;0;0;0;0;0;0;8;7;10;9;3;1]
                        | _ -> rule1.Weights

                    let firstCheck() =
                        standard weights number % 11 = 0

                    let secondCheck() =
                        let number = "309634" + accountNo
                        standard weights number % 11 = 0

                    // If the first row with exception 2 passes the standard modulus 11 check, you do not need to carry out the second check (ie it is deemed to be a valid sterling account).
                    firstCheck() || secondCheck()

                | Exception 5, Exception 5 ->
                    // Perform the first check (standard modulus check) except:
                    // If the sorting code appears in this table in the “Original s/c” column, substitute it for the “substitute with” column (for check purposes only).
                    // If the sorting code is not found, use the original sorting code.
                    let sortCode =
                        match substitutionTable |> List.tryFind (fun sub -> sub.SortCode = sortCode) with
                        | Some sub -> sub.SubstituteWith
                        | None -> sortCode
                    let number = sortCode + accountNo

                    let firstCheck() =
                        // For the standard check with exception 5 the checkdigit is g from the original account number.
                        // After dividing the result by 11:
                        // - if the remainder = 0 and g = 0 the account number is valid
                        // - if the remainder = 1 the account number is invalid
                        // - for all other remainders, take the remainder away from 11. If the number you get is the same as g then the account number is valid.
                        match standard rule1.Weights number % 11 with
                        | 0 when number.[Position.G] = '0' -> true
                        | 1 -> false
                        | reminder -> (11 - reminder) = char2int number.[Position.G]

                    let secondCheck() =
                        // Perform the second double alternate check, and for the double alternate check with exception 5 the checkdigit is h from the original account number, except:
                        // After dividing the result by 10:
                        // - if the remainder = 0 and h = 0 the account number is valid
                        // - for all other remainders, take the remainder away from 10. If the number you get is the same as h then the account number is valid.
                        match doubleAlternate rule1.Weights number % 10 with
                        | 0 when number.[Position.H] = '0' -> true
                        | reminder -> (10 - reminder) = char2int number.[Position.H]

                    firstCheck() && secondCheck()

                | Exception 6, Exception 6 ->
                    // Indicates that these sorting codes may contain foreign currency accounts which cannot be checked. Perform the first and second checks, except:
                    // If a = 4, 5, 6, 7 or 8, and g and h are the same, the accounts are for a foreign currency and the checks cannot be used.
                    if ("45678" |> Seq.contains number.[Position.A]) && number.[Position.G] = number.[Position.H]
                    then true
                    else validate rule1 number && validate rule2 number

                | Exception 10, Exception 11 ->
                    // These exceptions are for some Lloyds accounts and some TSB accounts. If there is a 10 in the exception column for the first check for a sorting code
                    // and an 11 in the exception column for the second check for the same sorting code, if either check is successful the account number is deemed valid.

                    let firstCheck() =
                        // For the exception 10 check, if ab = 09 or ab = 99 and g = 9, zeroise weighting positions u-b.
                        let rule1' =
                            if ["09"; "99"] |> List.contains number.[Position.A..Position.B] && number.[Position.G] = '9'
                            then { rule1 with Weights = [for i, w in rule1.Weights |> List.indexed -> if i >= Position.U && i <= Position.B then 0 else w] }
                            else rule1
                        validate rule1' number

                    let secondCheck() =
                        validate rule2 number

                    firstCheck() || secondCheck()

                | _ -> true

            | _ -> true

        if isValid then Valid else Invalid
