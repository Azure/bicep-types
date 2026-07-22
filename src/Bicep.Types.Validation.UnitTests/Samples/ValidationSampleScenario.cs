// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

/// <summary>
/// In-memory representation of a single <c>scenario.json</c> validation sample.
/// </summary>
public sealed class ValidationSampleScenario
{
    public ValidationSampleScenario(
        string resourcePrefix,
        string folderName,
        string name,
        string? description,
        string? category,
        IReadOnlyList<ValidationSampleInput> inputs,
        IReadOnlyList<string> modes)
    {
        ResourcePrefix = resourcePrefix;
        FolderName = folderName;
        Name = name;
        Description = description;
        Category = category;
        Inputs = inputs;
        Modes = modes;
    }

    /// <summary>Embedded-resource prefix up to (but excluding) <c>/scenario.json</c>.</summary>
    public string ResourcePrefix { get; }

    /// <summary>The scenario's containing folder name.</summary>
    public string FolderName { get; }

    /// <summary>Declared scenario name.</summary>
    public string Name { get; }

    /// <summary>Optional description.</summary>
    public string? Description { get; }

    /// <summary>Optional category label.</summary>
    public string? Category { get; }

    /// <summary>
    /// Declared input forms. Always contains at least one entry: when the scenario declares
    /// no explicit <c>inputs</c>, this defaults to a single <see cref="ValidationSampleInput.DefaultDirectory"/>.
    /// </summary>
    public IReadOnlyList<ValidationSampleInput> Inputs { get; }

    /// <summary>Declared validation modes.</summary>
    public IReadOnlyList<string> Modes { get; }
}
