namespace UkModulusCheck.CSharp

open UkModulusCheck
open UkModulusCheck.Types
open System

/// The result of validation
type ValidationResult = { IsValid: bool; FailureReason: Nullable<FailureReason> }

/// The C# API of the library
type Validator() =
    let mutable validationRules = None
    let mutable substitutions = None

    /// Loads rules for specific sort code ranges and sort code substitution table from files
    ///
    /// **Parameters**
    ///
    ///  * `rulesPath` - path to the file containing rules from VocaLink (valacdos.txt)
    ///  * `substitutionsPath` - path to the file containing substitutions from VocaLink (scsubtab.txt)
    ///
    /// **Output type**
    ///
    ///  * `void`
    ///
    /// **Exceptions**
    ///
    ///  * Exceptions may be thrown when file is not accesible (e.g. not found, lack of permissions)
    member this.LoadData rulesPath substitutionsPath =
        match Validator.loadRules rulesPath with
        | Ok rules -> validationRules <- Some rules
        | Error ex -> raise ex

        match Validator.loadSubstitutions substitutionsPath with
        | Ok subs -> substitutions <- Some subs
        | Error ex -> raise ex

    /// Runs modulus check on UK sort code and account number given the rules and substitution tables.
    /// Requires `LoadData` to be called first to initialize data tables.
    ///
    /// **Parameters**
    ///
    ///  * `sortCode` - sort code to check
    ///  * `accountNo` - account number to check
    ///
    /// **Output type**
    ///
    ///  * `ValidationResult` - describes the validation result and the reason in case of failure
    ///
    /// **Exceptions**
    ///
    ///  * Exception will be thrown when method is called before initializing the instance (calling `LoadData`).
    member this.ValidateAccount (sortCode: SortCode) (accountNo: AccountNumber) =
        match validationRules, substitutions with
        | Some rules, Some subs ->
            match Validator.validateAccountNo rules subs sortCode accountNo with
            | Valid -> { IsValid = true; FailureReason = Nullable() }
            | Invalid reason -> { IsValid = false; FailureReason = Nullable(reason) }
        | _ -> failwith "Class is not initialized! Call LoadData to initialize."

