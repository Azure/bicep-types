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
    public void Sample_matches_expected_baseline(string resourcePrefix, string name, string inputKind, string inputPath, string mode)
    {
        var expectedResourceName = ValidationSampleData.GetExpectedResultResourceName(resourcePrefix, mode);
        ValidationSampleData.ResourceExists(expectedResourceName).Should().BeTrue(
            $"scenario '{name}' declares mode '{mode}' and must have an expected result file at '{expectedResourceName}'.");

        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "bicep-types-validation-samples",
            Guid.NewGuid().ToString("N"));

        try
        {
            ValidationSampleData.MaterializePackage(
                resourcePrefix,
                Path.Combine(temporaryRoot, "package"));

            var input = ValidationSampleData.CreateValidationInput(
                ParseInputKind(inputKind),
                inputPath,
                temporaryRoot);
            var options = new TypePackageValidationOptions { Mode = ParseMode(mode) };

            var validator = new TypePackageValidator();
            var result = validator.Validate(input, options);

            var actual = ValidationSampleResultNormalizer.Normalize(result);
            var expected = ValidationSampleResultNormalizer.Canonicalize(
                ValidationSampleData.ReadResource(expectedResourceName));

            actual.Should().Be(
                expected,
                $"normalized result for scenario '{name}' via '{inputKind}' in mode '{mode}' should match baseline '{expectedResourceName}'.{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    private static ValidationSampleInputKind ParseInputKind(string kind) => kind switch
    {
        nameof(ValidationSampleInputKind.Directory) => ValidationSampleInputKind.Directory,
        nameof(ValidationSampleInputKind.IndexFile) => ValidationSampleInputKind.IndexFile,
        nameof(ValidationSampleInputKind.ArchiveFile) => ValidationSampleInputKind.ArchiveFile,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sample input kind."),
    };

    private static TypePackageValidationMode ParseMode(string mode) => mode switch
    {
        "canonicalWriter" => TypePackageValidationMode.CanonicalWriter,
        "compatibleReader" => TypePackageValidationMode.CompatibleReader,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown sample mode."),
    };
}
