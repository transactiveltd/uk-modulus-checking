namespace UkModulusCheck

module internal Helpers =
    open System
    open System.IO
    open System.Text.RegularExpressions

    module List =
        let sequence xs =
            if xs |> List.exists Option.isNone
            then None
            else Some (xs |> List.choose id)

    let char2int = Char.GetNumericValue >> int

    let str2int s =
        match Int32.TryParse(s) with
        | true, num -> Some num
        | _ -> None

    let trim (s: string) = s.Trim()

    let removeNonDigits s = String(s |> Seq.filter Char.IsDigit |> Seq.toArray)

    let (|Trimmed|) s =
        trim s

    let (|MatchesTrimmed|) regex s =
        Regex(regex).Match(trim s).Success

    let (|Not|_|) c input =
        if input <> c then Some c else None

    let loadLines parseLine lines =
        lines
        |> Seq.map (fun line -> if String.IsNullOrWhiteSpace line then None else parseLine line)
        |> Seq.choose id
        |> Seq.toList

    let loadFile parseLine path =
        try
            File.ReadAllLines(path)
            |> loadLines parseLine
            |> Ok
        with
        | e -> Error e
