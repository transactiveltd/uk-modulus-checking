# Usage from C#

## Prerequisites

To use the library from C# you'll need to add reference to `FSharp.Core` package to your project. You can [grab it from NuGet](https://www.nuget.org/packages/FSharp.Core).

## Usage

To check the sort code / account number pair you'll need:

* [Rules and weightings table](https://www.vocalink.com/media/3003/valacdos-v490.txt)
* [Sort code substitution table](https://www.vocalink.com/media/1517/scsubtab.txt).

You can obtain newest version of both files from [VocaLink](https://www.vocalink.com/customer-support/modulus-checking/).

Once you got the files, you need to create an instance of `Validator` class and initialize it by loading the data into memory using the `LoadData` method:

    [lang=csharp]
    using UkModulusCheck.CSharp;

    var validator = new Validator();
    try
    {
        validator.LoadData("valacdos.txt", "scsubtab.txt");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading data from files: {ex}");
    }

You can find the details of the method in [the reference docs](reference/ukmoduluscheck-csharp-validator.html).

To validate the sort code and account number pair use the `ValidateAccount` method:

    [lang=csharp]
    var sortCode = "000000";
    var accountNumber = "12345678";

    var result = validator.ValidateAccount(sortCode, accountNumber);

    if (result.IsValid)
        Console.WriteLine($"{sortCode}-{accountNumber} is valid :)");
    else
        Console.WriteLine($"{sortCode}-{accountNumber} is Invalid :( Reason: {result.FailureReason}");

For more information about the API and types used by the library please check [the reference docs](reference/index.html).

## Notes

Loading the data using the `LoadData` method modifies the state of the `Validator` class instance and should be considered thread unsafe.
Validating the data using the `ValidateAccount` method doesn't modify the state and can be considered thread safe.
