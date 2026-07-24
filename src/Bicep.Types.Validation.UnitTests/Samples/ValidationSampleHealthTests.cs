// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
}
