// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

[TestClass]
public class ValidationSampleHealthTests
{
    [TestMethod]
    public void Every_scenario_folder_name_matches_scenario_name()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            scenario.FolderName.Should().Be(
                scenario.Name,
                $"scenario folder '{scenario.ResourcePrefix}' should match its declared name.");
        }
    }

    [TestMethod]
    public void Every_declared_mode_has_an_expected_result_file()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            scenario.Modes.Should().NotBeEmpty($"scenario '{scenario.Name}' must declare at least one mode.");

            foreach (var mode in scenario.Modes)
            {
                var expected = ValidationSampleData.GetExpectedResultResourceName(scenario.ResourcePrefix, mode);
                ValidationSampleData.ResourceExists(expected).Should().BeTrue(
                    $"scenario '{scenario.Name}' declares mode '{mode}' but is missing '{expected}'.");
            }
        }
    }

    [TestMethod]
    public void No_expected_result_file_exists_for_an_undeclared_mode()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            foreach (var mode in ValidationSampleData.EnumerateExpectedModeResources(scenario.ResourcePrefix))
            {
                scenario.Modes.Should().Contain(
                    mode,
                    $"scenario '{scenario.Name}' has an expected result for mode '{mode}' that it does not declare.");
            }
        }
    }

    [TestMethod]
    public void Every_scenario_has_at_least_one_input_or_a_default_package_folder()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            scenario.Inputs.Should().NotBeEmpty(
                $"scenario '{scenario.Name}' should always resolve to at least one input (defaulting to 'package/').");

            ValidationSampleData.EnumeratePackageResources(scenario.ResourcePrefix).Should().NotBeEmpty(
                $"scenario '{scenario.Name}' should provide a default 'package/' folder.");
        }
    }

    [TestMethod]
    public void Every_scenario_json_and_expected_result_json_parses()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            var scenarioResource = $"{scenario.ResourcePrefix}/scenario.json";
            AssertParses(scenarioResource, $"scenario.json for '{scenario.Name}' should be valid JSON.");

            foreach (var mode in scenario.Modes)
            {
                var expected = ValidationSampleData.GetExpectedResultResourceName(scenario.ResourcePrefix, mode);
                AssertParses(expected, $"expected result for '{scenario.Name}' mode '{mode}' should be valid JSON.");
            }
        }
    }

    private static void AssertParses(string resourceName, string because)
    {
        Action parse = () =>
        {
            using var document = JsonDocument.Parse(ValidationSampleData.ReadResource(resourceName));
        };

        parse.Should().NotThrow(because);
    }

    [TestMethod]
    public void SampleData_discovers_archive_input_scenarios()
    {
        var scenarios = System.Linq.Enumerable.ToList(ValidationSampleData.EnumerateScenarios());

        scenarios.Should().Contain(
            s => System.Linq.Enumerable.Any(s.Inputs, i => i.Kind == ValidationSampleInputKind.ArchiveFile),
            "phase-6 samples include at least one archiveFile input scenario.");
    }

    [TestMethod]
    public void SampleData_materializes_archive_resources()
    {
        var scenario = System.Linq.Enumerable.First(
            ValidationSampleData.EnumerateScenarios(),
            s => System.Linq.Enumerable.Any(s.Inputs, i => i.Kind == ValidationSampleInputKind.ArchiveFile));

        var temporaryRoot = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bicep-types-validation-samples", Guid.NewGuid().ToString("N"));

        try
        {
            var packageRoot = ValidationSampleData.MaterializePackage(
                scenario.ResourcePrefix, System.IO.Path.Combine(temporaryRoot, "package"));
            var archivePath = System.IO.Path.Combine(temporaryRoot, "package.tgz");
            ValidationSampleData.MaterializeArchive(packageRoot, archivePath);

            System.IO.File.Exists(archivePath).Should().BeTrue();
            new System.IO.FileInfo(archivePath).Length.Should().BeGreaterThan(0);

            var result = new TypePackageValidator().Validate(
                TypePackageValidationInput.ForArchiveFile(archivePath));
            result.Diagnostics.Should().NotContain(
                d => d.Code == Azure.Bicep.Types.Validation.Diagnostics.TypeValidationDiagnosticCodes.ArchivePackageInvalid);
        }
        finally
        {
            if (System.IO.Directory.Exists(temporaryRoot))
            {
                System.IO.Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    // ── Phase-8 corpus health checks ─────────────────────────────────────────

    private static readonly HashSet<string> KnownModes =
        new(new[] { "canonicalWriter", "compatibleReader" }, StringComparer.Ordinal);

    [TestMethod]
    public void Every_scenario_has_non_empty_description_and_category()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            scenario.Description.Should().NotBeNullOrWhiteSpace(
                $"scenario '{scenario.Name}' must have a non-empty description.");
            scenario.Category.Should().NotBeNullOrWhiteSpace(
                $"scenario '{scenario.Name}' must have a non-empty category.");
        }
    }

    [TestMethod]
    public void Every_scenario_category_is_known()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            ValidationSampleCoverageReport.KnownCategories.Should().Contain(
                scenario.Category!,
                $"scenario '{scenario.Name}' uses category '{scenario.Category}', which is not in the known set.");
        }
    }

    [TestMethod]
    public void Every_scenario_mode_is_known()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            foreach (var mode in scenario.Modes)
            {
                KnownModes.Should().Contain(
                    mode,
                    $"scenario '{scenario.Name}' declares unknown mode '{mode}'.");
            }
        }
    }

    [TestMethod]
    public void Scenario_names_are_unique()
    {
        var duplicates = ValidationSampleData.EnumerateScenarios()
            .GroupBy(s => s.Name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            $"scenario names must be unique; duplicates: {string.Join(", ", duplicates)}.");
    }

    [TestMethod]
    public void Every_baseline_declares_the_matching_mode()
    {
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            foreach (var mode in scenario.Modes)
            {
                var expected = ValidationSampleData.GetExpectedResultResourceName(scenario.ResourcePrefix, mode);
                using var document = JsonDocument.Parse(ValidationSampleData.ReadResource(expected));

                document.RootElement.TryGetProperty("mode", out var modeElement).Should().BeTrue(
                    $"baseline '{expected}' must declare a 'mode' property.");
                modeElement.GetString().Should().Be(
                    mode,
                    $"baseline '{expected}' must declare mode '{mode}' matching its file name.");
            }
        }
    }

    [TestMethod]
    public void Every_baseline_satisfies_internal_invariants()
    {
        // These invariants assume default validation options (warnings included, no truncation),
        // which every current scenario uses. If a future scenario opts into warning filtering or
        // MaxDiagnostics, the summary would legitimately diverge from the returned diagnostics and
        // this check would need to account for that.
        foreach (var scenario in ValidationSampleData.EnumerateScenarios())
        {
            foreach (var mode in scenario.Modes)
            {
                var expected = ValidationSampleData.GetExpectedResultResourceName(scenario.ResourcePrefix, mode);
                using var document = JsonDocument.Parse(ValidationSampleData.ReadResource(expected));
                var root = document.RootElement;

                AssertProperty(root, "isValid", JsonValueKind.True, JsonValueKind.False, expected);
                AssertProperty(root, "mode", JsonValueKind.String, JsonValueKind.String, expected);
                AssertProperty(root, "diagnostics", JsonValueKind.Array, JsonValueKind.Array, expected);
                AssertProperty(root, "diagnosticsTruncated", JsonValueKind.True, JsonValueKind.False, expected);
                AssertProperty(root, "summary", JsonValueKind.Object, JsonValueKind.Object, expected);

                var summary = root.GetProperty("summary");
                var errorCount = summary.GetProperty("errorCount").GetInt32();
                var warningCount = summary.GetProperty("warningCount").GetInt32();
                var infoCount = summary.GetProperty("infoCount").GetInt32();

                var isValid = root.GetProperty("isValid").GetBoolean();
                isValid.Should().Be(
                    errorCount == 0,
                    $"baseline '{expected}' must have isValid == (errorCount == 0).");

                var severities = root.GetProperty("diagnostics").EnumerateArray()
                    .Select(d => d.GetProperty("severity").GetString())
                    .ToList();

                severities.Count(s => s == "error").Should().Be(
                    errorCount, $"baseline '{expected}' error count must match its diagnostics.");
                severities.Count(s => s == "warning").Should().Be(
                    warningCount, $"baseline '{expected}' warning count must match its diagnostics.");
                severities.Count(s => s == "info").Should().Be(
                    infoCount, $"baseline '{expected}' info count must match its diagnostics.");
            }
        }
    }

    private static void AssertProperty(
        JsonElement root, string name, JsonValueKind allowed1, JsonValueKind allowed2, string resource)
    {
        root.TryGetProperty(name, out var element).Should().BeTrue(
            $"baseline '{resource}' must declare a '{name}' property.");
        (element.ValueKind == allowed1 || element.ValueKind == allowed2).Should().BeTrue(
            $"baseline '{resource}' property '{name}' has unexpected JSON kind '{element.ValueKind}'.");
    }
}
