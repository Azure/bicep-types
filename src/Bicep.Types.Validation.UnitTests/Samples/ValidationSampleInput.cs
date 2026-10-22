// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

/// <summary>The input form declared by a sample scenario entry in <c>scenario.json#/inputs</c>.</summary>
public enum ValidationSampleInputKind
{
    /// <summary>An extracted package directory.</summary>
    Directory,

    /// <summary>A raw <c>index.json</c> file.</summary>
    IndexFile,

    /// <summary>A <c>types.tgz</c> archive file.</summary>
    ArchiveFile,
}

/// <summary>
/// In-memory representation of a single entry from <c>scenario.json#/inputs</c>.
/// </summary>
public sealed class ValidationSampleInput
{
    /// <summary>The default input used when a scenario declares no explicit inputs.</summary>
    public static readonly ValidationSampleInput DefaultDirectory =
        new(ValidationSampleInputKind.Directory, "package");

    public ValidationSampleInput(ValidationSampleInputKind kind, string path)
    {
        Kind = kind;
        Path = path;
    }

    /// <summary>The declared input form.</summary>
    public ValidationSampleInputKind Kind { get; }

    /// <summary>The input path, relative to the scenario's materialized root.</summary>
    public string Path { get; }
}
