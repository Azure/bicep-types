// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

[TestClass]
public class ValidationSampleDataTests
{
    [TestMethod]
    public void Scenario_without_inputs_defaults_to_directory_package()
    {
        var scenario = ValidationSampleData.ParseScenario(
            "prefix/sample",
            "sample",
            /*lang=json,strict*/ """{ "name": "sample", "modes": ["canonicalWriter"] }""");

        scenario.Inputs.Should().ContainSingle();
        scenario.Inputs[0].Kind.Should().Be(ValidationSampleInputKind.Directory);
        scenario.Inputs[0].Path.Should().Be("package");
    }

    [TestMethod]
    public void Explicit_input_override_is_honored()
    {
        var json = /*lang=json,strict*/ """
        {
          "name": "sample",
          "modes": ["canonicalWriter"],
          "inputs": [ { "kind": "indexFile", "path": "package/index.json" } ]
        }
        """;

        var scenario = ValidationSampleData.ParseScenario("prefix/sample", "sample", json);

        scenario.Inputs.Should().ContainSingle();
        scenario.Inputs[0].Kind.Should().Be(ValidationSampleInputKind.IndexFile);
        scenario.Inputs[0].Path.Should().Be("package/index.json");
    }

    [TestMethod]
    public void Multiple_explicit_inputs_are_all_parsed_in_order()
    {
        var json = /*lang=json,strict*/ """
        {
          "name": "sample",
          "modes": ["canonicalWriter"],
          "inputs": [
            { "kind": "directory", "path": "package" },
            { "kind": "archiveFile", "path": "types.tgz" }
          ]
        }
        """;

        var scenario = ValidationSampleData.ParseScenario("prefix/sample", "sample", json);

        scenario.Inputs.Select(i => i.Kind).Should().Equal(
            ValidationSampleInputKind.Directory,
            ValidationSampleInputKind.ArchiveFile);
        scenario.Inputs.Select(i => i.Path).Should().Equal("package", "types.tgz");
    }

    [TestMethod]
    public void Unsupported_input_kind_throws()
    {
        var json = /*lang=json,strict*/ """
        { "name": "sample", "modes": ["canonicalWriter"], "inputs": [ { "kind": "weird", "path": "p" } ] }
        """;

        Action act = () => ValidationSampleData.ParseScenario("prefix/sample", "sample", json);

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Input_missing_kind_or_path_throws()
    {
        var json = /*lang=json,strict*/ """
        { "name": "sample", "modes": ["canonicalWriter"], "inputs": [ { "kind": "directory" } ] }
        """;

        Action act = () => ValidationSampleData.ParseScenario("prefix/sample", "sample", json);

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Create_validation_input_maps_directory_kind_and_resolves_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "bicep-types-validation-input-test");

        var input = ValidationSampleData.CreateValidationInput(
            ValidationSampleInputKind.Directory,
            "package",
            root);

        input.DisplayPath.Should().Be(Path.Combine(root, "package"));
    }

    [TestMethod]
    public void Create_validation_input_maps_index_file_kind_and_resolves_nested_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "bicep-types-validation-input-test");

        var input = ValidationSampleData.CreateValidationInput(
            ValidationSampleInputKind.IndexFile,
            "package/index.json",
            root);

        input.DisplayPath.Should().Be(Path.Combine(root, "package", "index.json"));
    }

    [TestMethod]
    public void Sample_cases_expand_per_declared_input_and_mode()
    {
        // Every emitted case must map back to a real scenario input and declared mode.
        var scenarios = ValidationSampleData.EnumerateScenarios().ToList();
        var expectedCaseCount = scenarios.Sum(s => s.Inputs.Count * s.Modes.Count);

        var cases = ValidationSampleData.GetSampleCases().ToList();

        cases.Should().HaveCount(expectedCaseCount);
        cases.Should().OnlyContain(c => c.Length == 6);
    }
}
