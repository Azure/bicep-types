// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

/// <summary>
/// Explicit, opt-in baseline update workflow for the validation sample corpus. Normal test runs
/// never write source files. Update mode writes normalized results back to the source
/// <c>expected/&lt;mode&gt;.result.json</c> files, grouped per <c>(scenario, mode)</c> so a
/// multi-input scenario cannot silently overwrite one baseline with differing input results.
/// </summary>
public static class ValidationSampleBaselineUpdater
{
    /// <summary>Environment variable that enables baseline update mode when set to <c>true</c>.</summary>
    public const string SetBaselineEnvVar = "BICEP_TYPES_VALIDATION_SET_BASELINE";

    /// <summary>Environment variable that overrides the samples source root used for writes.</summary>
    public const string SamplesRootEnvVar = "BICEP_TYPES_VALIDATION_SAMPLES_ROOT";

    private const string ProjectFileName = "Bicep.Types.Validation.UnitTests.csproj";

    /// <summary>
    /// Whether a baseline update was explicitly requested, via the <see cref="SetBaselineEnvVar"/>
    /// environment variable or the supplied VSTest <c>SetBaseLine</c> run-parameter value.
    /// </summary>
    public static bool IsUpdateRequested(string? runParameterValue)
        => IsTrue(Environment.GetEnvironmentVariable(SetBaselineEnvVar)) || IsTrue(runParameterValue);

    private static bool IsTrue(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the samples source root deterministically without depending on absolute paths
    /// embedded in the build. Order: an explicit root, the <see cref="SamplesRootEnvVar"/>
    /// environment variable, then a walk upward from the test binaries to the project directory.
    /// Throws a clear, actionable error when the source tree cannot be located.
    /// </summary>
    public static string ResolveSamplesRoot(string? explicitRoot = null)
    {
        var candidate = !string.IsNullOrEmpty(explicitRoot)
            ? explicitRoot
            : Environment.GetEnvironmentVariable(SamplesRootEnvVar);

        if (!string.IsNullOrEmpty(candidate))
        {
            var full = Path.GetFullPath(candidate!);
            if (!Directory.Exists(full))
            {
                throw new DirectoryNotFoundException(
                    $"Samples root '{full}' (from an explicit value or {SamplesRootEnvVar}) does not exist.");
            }

            return full;
        }

        var projectDir = FindProjectDirectory(AppContext.BaseDirectory);
        if (projectDir is null)
        {
            throw new InvalidOperationException(
                $"Could not locate the samples source tree. Set the {SamplesRootEnvVar} environment variable to " +
                $"'<repo>/src/Bicep.Types.Validation.UnitTests/Files/validation-samples' and retry the baseline update.");
        }

        return Path.GetFullPath(Path.Combine(projectDir, "Files", "validation-samples"));
    }

    private static string? FindProjectDirectory(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ProjectFileName)))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Computes the source-tree write target for a scenario/mode baseline and verifies it stays
    /// under the canonical samples root. Rejects any resource prefix that escapes the root.
    /// </summary>
    public static string ComputeWriteTarget(string samplesRoot, string resourcePrefix, string mode)
    {
        var rootFull = Path.GetFullPath(samplesRoot);
        var relative = ToScenarioRelativePath(resourcePrefix);

        var target = Path.GetFullPath(Path.Combine(
            rootFull,
            relative.Replace('/', Path.DirectorySeparatorChar),
            "expected",
            $"{mode}.result.json"));

        var rootWithSeparator = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        if (!target.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to write baseline outside the samples root. Target '{target}' is not under '{rootFull}'.");
        }

        return target;
    }

    private static string ToScenarioRelativePath(string resourcePrefix)
    {
        if (resourcePrefix.StartsWith(ValidationSampleData.SampleRootResourcePrefix, StringComparison.Ordinal))
        {
            return resourcePrefix.Substring(ValidationSampleData.SampleRootResourcePrefix.Length);
        }

        return resourcePrefix;
    }

    /// <summary>
    /// Reduces per-input normalized results to a single agreed baseline. Returns <c>true</c> and the
    /// shared content when every input agrees; returns <c>false</c> and the index of the first
    /// divergent input otherwise. Throws when the input list is empty.
    /// </summary>
    public static bool TryReconcileInputs(
        IReadOnlyList<string> normalizedResults, out string agreed, out int firstDivergentIndex)
    {
        if (normalizedResults.Count == 0)
        {
            throw new ArgumentException("At least one input result is required.", nameof(normalizedResults));
        }

        agreed = normalizedResults[0];
        for (var i = 1; i < normalizedResults.Count; i++)
        {
            if (!string.Equals(normalizedResults[i], agreed, StringComparison.Ordinal))
            {
                firstDivergentIndex = i;
                return false;
            }
        }

        firstDivergentIndex = -1;
        return true;
    }

    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="fullPath"/> only when it differs from
    /// the existing file (comparing canonicalized JSON), creating the directory if needed. Returns
    /// <c>true</c> when a write occurred.
    /// </summary>
    public static bool WriteBaselineIfChanged(string fullPath, string content)
    {
        if (File.Exists(fullPath))
        {
            var existing = ValidationSampleResultNormalizer.Canonicalize(File.ReadAllText(fullPath));
            if (string.Equals(existing, content, StringComparison.Ordinal))
            {
                return false;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return true;
    }

    /// <summary>
    /// Runs the full corpus update against <paramref name="samplesRoot"/>. Each declared
    /// <c>(scenario, mode)</c> runs every input, requires identical normalized output, and writes
    /// the baseline exactly once. Returns a deterministic, human-readable summary of changes and
    /// mismatches.
    /// </summary>
    public static BaselineUpdateSummary UpdateCorpus(string samplesRoot)
    {
        var written = new List<string>();
        var unchanged = new List<string>();
        var mismatches = new List<string>();

        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            foreach (var mode in scenario.Modes)
            {
                var parsedMode = ValidationSampleData.ParseMode(mode);
                var results = scenario.Inputs
                    .Select(input => ValidationSampleData.RunScenarioNormalized(
                        scenario.ResourcePrefix, input.Kind, input.Path, parsedMode, scenario.ValidateUnreachableFiles))
                    .ToList();

                if (!TryReconcileInputs(results, out var agreed, out var divergentIndex))
                {
                    var divergentInput = scenario.Inputs[divergentIndex];
                    mismatches.Add(
                        $"{scenario.Name} [{mode}]: input '{divergentInput.Kind}:{divergentInput.Path}' " +
                        "produced a different normalized result than the first input; baseline not written.");
                    continue;
                }

                var target = ComputeWriteTarget(samplesRoot, scenario.ResourcePrefix, mode);
                var relativeTarget = $"{ToScenarioRelativePath(scenario.ResourcePrefix)}/expected/{mode}.result.json";

                if (WriteBaselineIfChanged(target, agreed))
                {
                    written.Add(relativeTarget);
                }
                else
                {
                    unchanged.Add(relativeTarget);
                }
            }
        }

        written.Sort(StringComparer.Ordinal);
        unchanged.Sort(StringComparer.Ordinal);
        mismatches.Sort(StringComparer.Ordinal);

        return new BaselineUpdateSummary(written, unchanged, mismatches);
    }

    /// <summary>Deterministic outcome of a corpus baseline update.</summary>
    public sealed class BaselineUpdateSummary
    {
        public BaselineUpdateSummary(
            IReadOnlyList<string> written,
            IReadOnlyList<string> unchanged,
            IReadOnlyList<string> mismatches)
        {
            Written = written;
            Unchanged = unchanged;
            Mismatches = mismatches;
        }

        /// <summary>Baselines that were rewritten because their content changed.</summary>
        public IReadOnlyList<string> Written { get; }

        /// <summary>Baselines that were already up to date.</summary>
        public IReadOnlyList<string> Unchanged { get; }

        /// <summary>Scenario/mode combinations skipped because their inputs disagreed.</summary>
        public IReadOnlyList<string> Mismatches { get; }
    }
}
