namespace AccountNoValidator

module Types =
    type ValidationResult = Valid | Invalid

    type ValidationMethod = Standard10 | Standard11 | DoubleAlternate

    type ValidationException = Exception of int

    type SortCode = string
    type AccountNumber = string
    type Weight = int

    type ValidationRule =
        {
            SortCodeFrom: SortCode
            SortCodeTo: SortCode
            Method: ValidationMethod
            Weights: Weight list
            Exception: ValidationException option
        }

    type SortCodeSubstitution = { SortCode: SortCode; SubstituteWith: SortCode }
