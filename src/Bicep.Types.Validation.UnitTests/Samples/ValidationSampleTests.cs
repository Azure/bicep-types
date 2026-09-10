// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

[TestClass]
public class ValidationSampleTests
{
    [TestMethod]
    public void SampleData_discovers_valid_canonical_scenarios()
    {
        var scenarios = System.Linq.Enumerable.ToList(ValidationSampleData.EnumerateScenarios());

        scenarios.Should().NotBeEmpty();
        scenarios.Should().Contain(s => s.Name == "minimal-resource");
    }

    [TestMethod]
    [DynamicData(
        nameof(ValidationSampleData.GetSampleCases),
        typeof(ValidationSampleData),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(ValidationSampleData.GetSampleCaseDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(ValidationSampleData))]
    public void Sample_matches_expected_baseline(string resourcePrefix, string name, string inputKind, string inputPath, string mode, bool validateUnreachableFiles)
    {
        var expectedResourceName = ValidationSampleData.GetExpectedResultResourceName(resourcePrefix, mode);
        ValidationSampleData.ResourceExists(expectedResourceName).Should().BeTrue(
            $"scenario '{name}' declares mode '{mode}' and must have an expected result file at '{expectedResourceName}'.");

        var actual = ValidationSampleData.RunScenarioNormalized(
            resourcePrefix,
            ParseInputKind(inputKind),
            inputPath,
            ValidationSampleData.ParseMode(mode),
            validateUnreachableFiles);

        var expected = ValidationSampleResultNormalizer.Canonicalize(
            ValidationSampleData.ReadResource(expectedResourceName));

        actual.Should().Be(
            expected,
            $"normalized result for scenario '{name}' via '{inputKind}' in mode '{mode}' should match baseline '{expectedResourceName}'.{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
    }

    private static ValidationSampleInputKind ParseInputKind(string kind) => kind switch
    {
        nameof(ValidationSampleInputKind.Directory) => ValidationSampleInputKind.Directory,
        nameof(ValidationSampleInputKind.IndexFile) => ValidationSampleInputKind.IndexFile,
        nameof(ValidationSampleInputKind.ArchiveFile) => ValidationSampleInputKind.ArchiveFile,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sample input kind."),
    };
}
