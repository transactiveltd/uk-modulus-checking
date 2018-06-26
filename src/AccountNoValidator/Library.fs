namespace AccountNoValidator

module Validator =
    open Types

    // TODO
    // * Validate input SC/AN
    // * Normalize AN to 8 digits
    // * Implement validation methods
    // * Implement exceptions
    // * Check if there is rule for given SC

    let calculateChecksum (method: ValidationMethod) (weights: Weight list) (sortCode: SortCode) (accountNo: AccountNumber) =
        //Q: is the algorithm the same as validation method?
        //Q: how will the weights be provided?
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
        | rules -> Implementation.validateRules rules (sortCode + accountNo)

