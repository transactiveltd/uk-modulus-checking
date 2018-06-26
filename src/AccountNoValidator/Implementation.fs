namespace AccountNoValidator

module internal Implementation =
    open System
    open Types

    module Positions =
        let SC_1_u: Weight = 0
        let SC_2_v: Weight = 1
        let SC_3_w: Weight = 2
        let SC_4_x: Weight = 3
        let SC_5_y: Weight = 4
        let SC_6_z: Weight = 5
        let AN_1_a: Weight = 6
        let AN_2_b: Weight = 7
        let AN_3_c: Weight = 8
        let AN_4_d: Weight = 9
        let AN_5_e: Weight = 10
        let AN_6_f: Weight = 11
        let AN_7_g: Weight = 12
        let AN_8_h: Weight = 13

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

    let validateRules (rules: ValidationRule list) number =
        let exceptions = rules |> List.choose (fun r -> r.Exception)
        let isValid =
            match rules, exceptions with
            | [rule], [] -> validate rule number
            | [rule], [ex] ->
                match ex with
                | Exception 1 ->
                    // Perform the double alternate check except: Add 27 to the total (ie before you divide by 10)
                    (doubleAlternate rule.Weights number + 27) % 10 = 0
                | Exception 3 ->
                    // If c=6 or c=9 the double alternate check does not need to be carried out.
                    match number.[Positions.AN_3_c] with
                    | '6' | '9' -> true
                    | _ -> validate rule number
                | Exception 4 ->
                    // Perform the standard modulus 11 check.
                    // After you have finished the check, ensure that the remainder is the same as the two-digit checkdigit;
                    // the checkdigit for exception 4 is gh from the original account number.
                    let modulus = standard rule.Weights number % 11
                    let checkdigit = (char2int number.[Positions.AN_7_g]) * 10 + (char2int number.[Positions.AN_8_h])
                    modulus = checkdigit
                | Exception 7 ->
                    // Perform the check as specified, except if g = 9 zeroise weighting positions u-b.
                    let rule', number' =
                        if number.[Positions.AN_7_g] = '9'
                        then { rule with Weights = rule.Weights.[Positions.AN_2_b+1..] }, number.[Positions.AN_2_b+1..]
                        else rule, number
                    validate rule' number'
                | Exception 8 ->
                    // Perform the check as specified, except substitute the sorting code with 090126, for check purposes only.
                    validate rule ("090126" + number.[6..])
                | _ -> true
            | [rule1; rule2], [ex1; ex2] -> validate rule1 number
            | _ -> true
        if isValid then Valid else Invalid
