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
    let validateAccountNo (rulesTable: ValidationRule seq) (substitutionTable: SortCodeSubstitution seq) (sortCode: SortCode) (accountNo: AccountNumber) =
        validateSortCode sortCode
        |> ValidationResult.bind (fun () -> validateAccountNo accountNo)
        |> ValidationResult.bind (fun () ->
            let sortCode', accountNo' = standardise (sortCode |> removeNonDigits) (accountNo |> trim)
            let rules = rulesTable |> Seq.filter (fun r -> sortCode' >= r.SortCodeFrom && sortCode' <= r.SortCodeTo) |> Seq.toList
            let substitutions = substitutionTable |> Seq.toList
            validateRules rules substitutions sortCode' accountNo'
            )

