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
}
