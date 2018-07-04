namespace UkModulusCheck.CSharp

open UkModulusCheck
open UkModulusCheck.Types
open System
open System.Collections.Generic

/// The result of validation
type ValidationResult = { IsValid: bool; FailureReason: Nullable<FailureReason> }

/// The C# API of the library
type Validator() =

    /// Loads rules for specific sort code ranges from file (valacdos.txt)
    ///
    /// **Parameters**
    ///
    ///  * `path` - path to the file containing rules from VocaLink
    ///
    /// **Output type**
    ///
    ///  * `List<ValidationRule>`
    ///
    /// **Exceptions**
    ///
    ///  * Exceptions may be thrown when file is not accesible (e.g. not found, lack of permissions)
    static member LoadRules path =
        match Validator.loadRules path with
        | Ok rules -> List<ValidationRule>(rules)
        | Error ex -> raise ex

    /// Loads sort code substitution table from file (scsubtab.txt)
    ///
    /// **Parameters**
    ///
    ///  * `path` - path to the file containing substitutions from VocaLink
    ///
    /// **Output type**
    ///
    ///  * `List<SortCodeSubstitution>`
    ///
    /// **Exceptions**
    ///
    ///  * Exceptions may be thrown when file is not accesible (e.g. not found, lack of permissions)
    static member LoadSubstitutions path =
        match Validator.loadSubstitutions path with
        | Ok subs -> List<SortCodeSubstitution>(subs)
        | Error ex -> raise ex

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
    static member ValidateAccountNo (rulesTable: ValidationRule seq) (substitutionTable: SortCodeSubstitution seq) (sortCode: SortCode) (accountNo: AccountNumber) =
        match Validator.validateAccountNo rulesTable substitutionTable sortCode accountNo with
        | Valid -> { IsValid = true; FailureReason = Nullable() }
        | Invalid reason -> { IsValid = false; FailureReason = Nullable(reason) }

