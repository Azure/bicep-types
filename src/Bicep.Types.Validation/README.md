# Azure.Bicep.Types.Validation

`Azure.Bicep.Types.Validation` validates serialized Bicep type packages before they are published or consumed. It checks package layout and JSON structure. It also checks reference placement and resolution. Diagnostics are returned as structured data with stable codes, severity, message and source locations.

The validator accepts:

- An extracted package directory.
- A raw `index.json` file.
- A `types.tgz` archive from a file or stream.

## Usage

Reference the `Azure.Bicep.Types.Validation` package, create an input, and pass it to an `ITypePackageValidator`:

```csharp
using Azure.Bicep.Types.Validation;

var input = TypePackageValidationInput.ForArchiveFile("types.tgz");
var options = new TypePackageValidationOptions
{
    Mode = TypePackageValidationMode.CanonicalWriter,
    ValidateUnreachableFiles = true,
};

ITypePackageValidator validator = new TypePackageValidator();
var result = validator.Validate(input, options);

foreach (var diagnostic in result.Diagnostics)
{
    Console.Error.WriteLine(
        $"{diagnostic.Path}{diagnostic.JsonPointer}: " +
        $"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}");
}

if (!result.IsValid)
{
    Environment.ExitCode = 1;
}
```

Applications can register `TypePackageValidator` as the implementation of `ITypePackageValidator` for dependency injection. Callers that do not use dependency injection can continue to instantiate `TypePackageValidator` directly.

Choose the input factory that matches the package source:

```csharp
TypePackageValidationInput.ForDirectory("path/to/package");
TypePackageValidationInput.ForIndexFile("path/to/package/index.json");
TypePackageValidationInput.ForArchiveFile("path/to/types.tgz");
TypePackageValidationInput.ForArchiveStream(stream, "types.tgz");
```

Directory and archive inputs must contain an `index.json` at the package root; otherwise validation reports `BCPVT001`. In both input forms, type files that are not reachable from `index.json` are ignored by default; set `ValidateUnreachableFiles` to report and validate them.

Package-relative reference paths use one OS-independent identity. Safe aliases are canonicalized by
converting `\` to `/`, removing exact `.` segments, and collapsing repeated internal separators. For
example, `./common//types.json` becomes `common/types.json`. Rooted paths, drive-qualified paths,
exact `..` segments, empty file paths, and trailing separators are rejected. Path case is preserved,
while package identity retains its existing case-insensitive comparison. Raw archive member names
remain stricter: noncanonical member names are rejected so normalization cannot hide collisions.

Entry-point references in `index.json` use the cross-file `<path>#/<index>` form. References inside
type objects use the same-file `#/<index>` form. Both validation modes reject a reference form used
in the wrong document.

### Validation modes

- `CanonicalWriter` enforces the serialized form that package producers should emit. This is the default and the recommended mode for publishing workflows.
- `CompatibleReader` accepts documented legacy forms that readers continue to support. Some canonical errors are reported as warnings in this mode.

Mode and format version are independent. `BicepTypesV1` is the current supported format and the default value of `TypePackageValidationOptions.FormatVersion`.

### Options and results

`TypePackageValidationOptions` also controls warning and informational diagnostic inclusion, validation of unreachable package files, and the maximum number of returned diagnostics. Package hygiene is opt-in through `ValidateUnreachableFiles` because it examines files outside the graph reachable from `index.json`.

Archive inputs are read incrementally and use resource limits to prevent compressed or expanded
content from consuming unbounded memory. The defaults are 64 MiB of compressed input, 256 MiB of
expanded tar content, 32 MiB per package file, and 4096 package files. Callers that intentionally
validate larger packages can provide custom limits:

```csharp
var options = new TypePackageValidationOptions
{
    ArchiveLimits = new TypePackageArchiveLimits(
        maxCompressedArchiveBytes: 128L * 1024L * 1024L,
        maxExpandedArchiveBytes: 512L * 1024L * 1024L,
        maxPackageFileBytes: 64L * 1024L * 1024L,
        maxPackageFileCount: 8192),
};
```

These limits apply only to `types.tgz` file and stream inputs. Exceeding any archive limit produces
the fatal archive diagnostic `BCPVT029` and stops validation before package JSON is processed.

`TypePackageValidationResult` provides:

- `IsValid`, based on every detected error before filtering or truncation.
- `Diagnostics`, sorted deterministically after the requested filtering and limit are applied.
- `DiagnosticsTruncated`, indicating that `MaxDiagnostics` shortened the returned list.
- `Summary`, containing error, warning, and informational counts from the complete validation run.

## Project structure

`TypePackageValidator` coordinates the validation pipeline. The main folders align with each stage:

| Area | Responsibility |
| --- | --- |
| `Packaging/` | Resolves inputs, reads package files, and safely expands archive contents into an in-memory file system. |
| `Structural/` | Validates JSON document shapes, required fields, discriminators, and reference syntax. |
| `Graph/` | Loads referenced type files and validates cross-file targets and reachability. |
| `Semantic/` | Checks scalar values, ranges, flags, and other value-domain constraints. |
| `Policy/` | Applies canonical-writer and compatible-reader format policy. |
| `Hygiene/` | Validates unreachable files and unexpected package members when enabled. |
| `Diagnostics/` | Defines diagnostic codes, severities, locations, builders, and deterministic ordering. |

Public input, option, mode, version, result, and summary types live at the project root. Tests and the structured sample corpus are maintained in `Bicep.Types.Validation.UnitTests`.

## Validation behavior

Validation collects diagnostics across independent stages when the package can be read safely. Fatal input or archive errors stop later stages because no usable package model is available. Archive member paths are checked before extraction, and archive validation uses the same structural and semantic pipeline as directory validation.

Diagnostic codes are the stable integration surface for automation. Messages and source locations provide review context.
