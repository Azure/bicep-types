// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Azure.Bicep.Types.Validation;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

/// <summary>
/// Discovers validation sample scenarios from embedded resources and provides IO helpers
/// for the sample baseline harness.
/// </summary>
public static class ValidationSampleData
{
    private const string SampleRoot = "Files/validation-samples/";
    private const string ScenarioFileSuffix = "/scenario.json";
    private const string ExpectedResultSuffix = ".result.json";

    private static Assembly SampleAssembly => typeof(ValidationSampleData).Assembly;

    /// <summary>Enumerates every discovered scenario, ordered deterministically.</summary>
    public static IEnumerable<ValidationSampleScenario> EnumerateScenarios()
    {
        var scenarioResources = SampleAssembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(SampleRoot, StringComparison.Ordinal)
                && n.EndsWith(ScenarioFileSuffix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal);

        foreach (var resourceName in scenarioResources)
        {
            var prefix = resourceName.Substring(0, resourceName.Length - ScenarioFileSuffix.Length);
            var folderName = prefix.Substring(prefix.LastIndexOf('/') + 1);

            yield return ParseScenario(prefix, folderName, ReadResource(resourceName));
        }
    }

    /// <summary>
    /// Parses a single <c>scenario.json</c> document into a <see cref="ValidationSampleScenario"/>.
    /// When no explicit <c>inputs</c> are declared, the scenario defaults to a single
    /// directory input pointing at <c>package/</c>.
    /// </summary>
    public static ValidationSampleScenario ParseScenario(string resourcePrefix, string folderName, string scenarioJson)
    {
        using var document = JsonDocument.Parse(scenarioJson);
        var root = document.RootElement;

        var name = root.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString() ?? folderName
            : folderName;
        var description = root.TryGetProperty("description", out var descriptionElement)
            ? descriptionElement.GetString()
            : null;
        var category = root.TryGetProperty("category", out var categoryElement)
            ? categoryElement.GetString()
            : null;

        var inputs = ParseInputs(folderName, root);

        var modes = new List<string>();
        if (root.TryGetProperty("modes", out var modesElement)
            && modesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var modeElement in modesElement.EnumerateArray())
            {
                var mode = modeElement.GetString();
                if (!string.IsNullOrEmpty(mode))
                {
                    modes.Add(mode!);
                }
            }
        }

        return new ValidationSampleScenario(resourcePrefix, folderName, name, description, category, inputs, modes);
    }

    private static IReadOnlyList<ValidationSampleInput> ParseInputs(string folderName, JsonElement root)
    {
        var inputs = new List<ValidationSampleInput>();

        if (root.TryGetProperty("inputs", out var inputsElement)
            && inputsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var inputElement in inputsElement.EnumerateArray())
            {
                var kind = inputElement.TryGetProperty("kind", out var kindElement)
                    ? kindElement.GetString()
                    : null;
                var path = inputElement.TryGetProperty("path", out var pathElement)
                    ? pathElement.GetString()
                    : null;

                if (string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException(
                        $"Scenario '{folderName}' declares an input missing 'kind' or 'path'.");
                }

                inputs.Add(new ValidationSampleInput(ParseInputKind(folderName, kind!), path!));
            }
        }

        if (inputs.Count == 0)
        {
            inputs.Add(ValidationSampleInput.DefaultDirectory);
        }

        return inputs;
    }

    private static ValidationSampleInputKind ParseInputKind(string folderName, string kind) => kind switch
    {
        "directory" => ValidationSampleInputKind.Directory,
        "indexFile" => ValidationSampleInputKind.IndexFile,
        "archiveFile" => ValidationSampleInputKind.ArchiveFile,
        _ => throw new InvalidOperationException(
            $"Scenario '{folderName}' declares an unsupported input kind '{kind}'."),
    };

    /// <summary>
    /// Builds a <see cref="TypePackageValidationInput"/> for a sample input, resolving its
    /// declared path relative to the materialized scenario <paramref name="materializedRoot"/>.
    /// </summary>
    public static TypePackageValidationInput CreateValidationInput(
        ValidationSampleInputKind kind,
        string inputPath,
        string materializedRoot)
    {
        var resolvedPath = Path.Combine(
            materializedRoot,
            inputPath.Replace('/', Path.DirectorySeparatorChar));

        return kind switch
        {
            ValidationSampleInputKind.Directory => TypePackageValidationInput.ForDirectory(resolvedPath),
            ValidationSampleInputKind.IndexFile => TypePackageValidationInput.ForIndexFile(resolvedPath),
            ValidationSampleInputKind.ArchiveFile => TypePackageValidationInput.ForArchiveFile(resolvedPath),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported sample input kind."),
        };
    }

    /// <summary>DynamicData source: one case per (scenario, input, mode).</summary>
    public static IEnumerable<object[]> GetSampleCases()
    {
        foreach (var scenario in EnumerateScenarios())
        {
            foreach (var input in scenario.Inputs)
            {
                foreach (var mode in scenario.Modes)
                {
                    yield return new object[]
                    {
                        scenario.ResourcePrefix,
                        scenario.Name,
                        input.Kind.ToString(),
                        input.Path,
                        mode,
                    };
                }
            }
        }
    }

    /// <summary>DynamicData display name formatter.</summary>
    public static string GetSampleCaseDisplayName(MethodInfo methodInfo, object[] data)
        => $"{data[1]} [{data[2]} -> {data[4]}]";

    /// <summary>Reads an embedded resource as text.</summary>
    public static string ReadResource(string resourceName)
    {
        using var stream = SampleAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Whether an embedded resource with the given name exists.</summary>
    public static bool ResourceExists(string resourceName)
        => SampleAssembly.GetManifestResourceNames().Contains(resourceName, StringComparer.Ordinal);

    /// <summary>Builds the expected-result resource name for a scenario and mode.</summary>
    public static string GetExpectedResultResourceName(string resourcePrefix, string mode)
        => $"{resourcePrefix}/expected/{mode}{ExpectedResultSuffix}";

    /// <summary>Enumerates the package resources for a scenario.</summary>
    public static IReadOnlyList<string> EnumeratePackageResources(string resourcePrefix)
    {
        var packagePrefix = $"{resourcePrefix}/package/";
        return SampleAssembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(packagePrefix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Enumerates the modes for which an expected-result file exists on disk.</summary>
    public static IEnumerable<string> EnumerateExpectedModeResources(string resourcePrefix)
    {
        var expectedPrefix = $"{resourcePrefix}/expected/";
        foreach (var resourceName in SampleAssembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(expectedPrefix, StringComparison.Ordinal)
                && n.EndsWith(ExpectedResultSuffix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal))
        {
            var fileName = resourceName.Substring(expectedPrefix.Length);
            yield return fileName.Substring(0, fileName.Length - ExpectedResultSuffix.Length);
        }
    }

    /// <summary>
    /// Materializes a scenario's <c>package/</c> resources under <paramref name="packageRoot"/>
    /// and returns the package root path.
    /// </summary>
    public static string MaterializePackage(string resourcePrefix, string packageRoot)
    {
        // Always create the package directory so scenarios with an intentionally empty
        // package dir (e.g. missing-index-file) still receive an existing directory.
        Directory.CreateDirectory(packageRoot);

        var packagePrefix = $"{resourcePrefix}/package/";
        foreach (var resourceName in EnumeratePackageResources(resourcePrefix))
        {
            var relativePath = resourceName.Substring(packagePrefix.Length);
            var destinationPath = Path.Combine(
                packageRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            using var stream = SampleAssembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
            using var file = File.Create(destinationPath);
            stream.CopyTo(file);
        }

        return packageRoot;
    }
}
