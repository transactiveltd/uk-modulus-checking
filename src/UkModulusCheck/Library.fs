namespace UkModulusCheck

/// The API of the library
module Validator =
    open Types
    open Helpers
    open Implementation

    /// Loads rules for specific sort code ranges from file (valacdos.txt)
    ///
    /// **Parameters**
    ///
    ///  * `path` - path to the file containing rules from VocaLink
    ///
    /// **Output type**
    ///
    ///  * `Result<ValidationRule list, exn>`
    let loadRules path =
        loadFile parseValidationRule path

    /// Loads sort code substitution table from file (scsubtab.txt)
    ///
    /// **Parameters**
    ///
    ///  * `path` - path to the file containing substitutions from VocaLink
    ///
    /// **Output type**
    ///
    ///  * `Result<SortCodeSubstitution list, exn>`
    let loadSubstitutions path =
        loadFile parseSubstitution path

    /// Runs modulus check on UK sort code and account number given the rules and substitution tables.
    ///
    /// **Parameters**
    ///
    ///  * `rulesTable` - collection containing information about which algorithm and weightings to use for given sort code
    ///  * `substitutionTable` - collection containing sort codes which need to be substituted for check purposes
    ///  * `sortCode` - sort code to check
    ///  * `accountNo` - account number to check
    ///
    /// **Output type**
    ///
    ///  * `ValidationResult`
    let validateAccountNo (rulesTable: ValidationRule list) (substitutionTable: SortCodeSubstitution list) (sortCode: SortCode) (accountNo: AccountNumber) =
        //Q: is any cleanup on the input data required (especially if they are to be strings)?
        //Q: what is the expected output - just the information if the SC/AN is valid or something more is needed?
        match standardise sortCode accountNo with
        | Some (sortCode, accountNo) ->
            let rules = rulesTable |> List.filter (fun r -> sortCode >= r.SortCodeFrom && sortCode <= r.SortCodeTo)
            validateRules rules substitutionTable sortCode accountNo
        | None ->
            Invalid InvalidInput

