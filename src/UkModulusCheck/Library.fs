namespace UkModulusCheck

/// The API of the library
module Validator =
    open Types
    open Helpers
    open Implementation

    // TODO
    // * Validate input SC/AN

    /// Loads rules for specific sort code ranges from file (valacdos.txt)
    let loadRules path =
        loadFile parseValidationRule path

    /// Loads sort code substitution table from file (scsubtab.txt)
    let loadSubstitutions path =
        loadFile parseSubstitution path

    let calculateChecksum (method: ValidationMethod) (weightings: Weight list) (sortCode: SortCode) (accountNo: AccountNumber) =
        //Q: is the algorithm the same as validation method?
        //Q: how will the weightings be provided?
        //Q: what will be the input type for SC/AN?
        //Q: what is the expected output - just a check digit as an int?
        0

    /// Runs modulus check on UK sort code and account number given the rules and substitution tables
    let validateAccountNo (rulesTable: ValidationRule list) (substitutionTable: SortCodeSubstitution list) (sortCode: SortCode) (accountNo: AccountNumber) =
        //Q: is any cleanup on the input data required (especially if they are to be strings)?
        //Q: what is the expected output - just the information if the SC/AN is valid or something more is needed?
        match standardise sortCode accountNo with
        | Some (sortCode, accountNo) ->
            let rules = rulesTable |> List.filter (fun r -> sortCode >= r.SortCodeFrom && sortCode <= r.SortCodeTo)
            validateRules rules substitutionTable sortCode accountNo
        | None ->
            Invalid

