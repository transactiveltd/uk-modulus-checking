namespace UkModulusCheck

module Validator =
    open System
    open System.IO
    open Types
    open Implementation

    // TODO
    // * Validate input SC/AN

    let loadRules path =
        let mapMethod = function
        | "MOD10" -> Some Standard10
        | "MOD11" -> Some Standard11
        | "DBLAL" -> Some DoubleAlternate
        | _ -> None

        let str2int s =
            match Int32.TryParse(s) with
            | true, num -> Some num
            | _ -> None

        let parseLine line =
            if String.IsNullOrWhiteSpace line
            then None
            else
                match line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) with
                | xs when xs.Length = 18 ->
                    let scFrom, scTo = xs.[0], xs.[1]
                    let method = xs.[2] |> mapMethod
                    let weightings = xs.[3..16] |> Array.toList |> List.map str2int |> List.sequence
                    let ex = str2int xs.[17]
                    Option.map3 (fun m ws e -> { SortCodeFrom = scFrom; SortCodeTo = scTo; Method = m; Weightings = ws; Exception = Some (Exception e) }) method weightings ex
                | xs when xs.Length = 17 ->
                    let scFrom, scTo = xs.[0], xs.[1]
                    let method = xs.[2] |> mapMethod
                    let weightings = xs.[3..16] |> Array.toList |> List.map str2int |> List.sequence
                    Option.map2 (fun m ws -> { SortCodeFrom = scFrom; SortCodeTo = scTo; Method = m; Weightings = ws; Exception = None }) method weightings
                | _ -> None

        try
            File.ReadAllLines(path)
            |> Array.map parseLine
            |> Array.choose id
            |> Array.toList
            |> Ok
        with
        | e -> Error e

    let loadSubstitutions path =
        let parseLine line =
            if String.IsNullOrWhiteSpace line
            then None
            else
                match line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) with
                | [|scFrom; scTo|] -> Some { SortCode = scFrom; SubstituteWith = scTo }
                | _ -> None
        try
            File.ReadAllLines(path)
            |> Array.map parseLine
            |> Array.choose id
            |> Array.toList
            |> Ok
        with
        | e -> Error e

    let calculateChecksum (method: ValidationMethod) (weightings: Weight list) (sortCode: SortCode) (accountNo: AccountNumber) =
        //Q: is the algorithm the same as validation method?
        //Q: how will the weightings be provided?
        //Q: what will be the input type for SC/AN?
        //Q: what is the expected output - just a check digit as an int?
        0

    let validateAccountNo (rulesTable: ValidationRule list) (substitutionTable: SortCodeSubstitution list) (sortCode: SortCode) (accountNo: AccountNumber) =
        //Q: what will be the input to the function (pair SC/AN, just a string/int/?, something else?)
        //Q: is any cleanup on the input data required (especially if they are to be strings)?
        //Q: what is the expected output - just the information if the SC/AN is valid or something more is needed?
        let rules = rulesTable |> List.filter (fun r -> sortCode >= r.SortCodeFrom && sortCode <= r.SortCodeTo)
        match rules with
        | [] -> Valid
        | rules -> validateRules rules substitutionTable sortCode accountNo

