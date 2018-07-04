# Usage from C#

## Prerequisites

To use the library from C# you'll need to add reference to `FSharp.Core` package to your project. You can [grab it from NuGet](https://www.nuget.org/packages/FSharp.Core).

## Usage

To check the sort code / account number pair you'll need:

* [Rules and weightings table](https://www.vocalink.com/media/3003/valacdos-v490.txt)
* [Sort code substitution table](https://www.vocalink.com/media/1517/scsubtab.txt).

You can obtain newest version of both files from [VocaLink](https://www.vocalink.com/customer-support/modulus-checking/).

Once you got the files, you can load the data into memory (and potentially cache it) using provided `Validator` class:

    [lang=csharp]
    using UkModulusCheck.CSharp;

    try
    {
        var rules = Validator.LoadRules("valacdos-v490.txt");
        var substitutions = Validator.LoadSubstitutions("scsubtab.txt");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading data from files: {ex}");
    }

You can find the details of those methods in [the reference docs](reference/ukmoduluscheck-csharp-validator.html).

To validate the sort code and account number pair use the `ValidateAccountNo` method:

    [lang=csharp]
    var result = Validator.ValidateAccountNo(rules, substitutions, sortCode, accountNumber);

    if (result.IsValid)
        Console.WriteLine($"{sortCode}-{accountNumber} is valid :)");
    else
        Console.WriteLine($"{sortCode}-{accountNumber} is Invalid :( Reason: {result.FailureReason}");

For more information about the API and types used by the library please check [the reference docs](reference/index.html).

