namespace UkModulusCheck

module internal Helpers =
    open System
    open System.IO

    module List =
        let sequence xs =
            if xs |> List.exists Option.isNone
            then None
            else Some (xs |> List.choose id)

    let (|Not|_|) c input =
        if input <> c then Some c else None

    let char2int = Char.GetNumericValue >> int

    let str2int s =
        match Int32.TryParse(s) with
        | true, num -> Some num
        | _ -> None

    let isDigitString s = s |> Seq.forall Char.IsDigit

    let loadFile parseLine path =
        try
            File.ReadAllLines(path)
            |> Array.map (fun line -> if String.IsNullOrWhiteSpace line then None else parseLine line)
            |> Array.choose id
            |> Array.toList
            |> Ok
        with
        | e -> Error e
