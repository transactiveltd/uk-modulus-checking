namespace UkModulusCheck

/// Types used by the API of the library
module Types =

    /// The result of the validation
    type ValidationResult = Valid | Invalid of FailureReason
    /// The reason of validation failure
    and FailureReason =
        ModulusCheckFailed
        | UnrecognizedRule
        | SortCodeInvalidLength
        | SortCodeInvalidFormat
        | AccountNumberInvalidLength
        | AccountNumberInvalidFormat

    module ValidationResult =
        let bind f = function
            | Valid -> f()
            | invalid -> invalid

    /// The available validation algorithms
    type ValidationMethod = Standard10 | Standard11 | DoubleAlternate

    /// The exeception from the standard validation rules as described in the VocaLink documentation
    type ValidationException = Exception of int

    /// Alias type for sort code
    type SortCode = string
    /// Alias type for account number
    type AccountNumber = string
    /// Alias type for position weight
    type Weight = int

    /// Captures the parsed line from from the validation rules table provided by VocaLink
    type ValidationRule =
        {
            SortCodeFrom: SortCode
            SortCodeTo: SortCode
            Method: ValidationMethod
            Weightings: Weight list
            Exception: ValidationException option
        }

    /// Captures the parsed line from from the sort code substitutions table provided by VocaLink
    type SortCodeSubstitution = { SortCode: SortCode; SubstituteWith: SortCode }
