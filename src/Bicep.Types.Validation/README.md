# Azure.Bicep.Types.Validation

`Azure.Bicep.Types.Validation` validates serialized Bicep type packages. It can validate an extracted package directory, a raw `index.json` file, or a `types.tgz` archive.

```csharp
using Azure.Bicep.Types.Validation;

var validator = new TypePackageValidator();

var result = validator.Validate(
    TypePackageValidationInput.ForArchiveFile("types.tgz"),
    new TypePackageValidationOptions
    {
        Mode = TypePackageValidationMode.CanonicalWriter,
        ValidateUnreachableFiles = true,
    });

if (!result.IsValid)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
    }
}
```

Use `CanonicalWriter` mode for packages being produced or published. Use `CompatibleReader` mode only when intentionally validating documented legacy package forms that current readers still accept.
