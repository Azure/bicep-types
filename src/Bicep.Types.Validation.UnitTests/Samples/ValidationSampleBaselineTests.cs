// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

/// <summary>
/// Opt-in baseline maintenance. These tests never write source files during a normal run; the
/// update test only writes when <c>SetBaseLine=true</c> is passed as a VSTest run parameter or the
/// <see cref="ValidationSampleBaselineUpdater.SetBaselineEnvVar"/> environment variable is set.
/// </summary>
/// <remarks>
/// To update baselines locally:
/// <code>
/// dotnet test src/Bicep.Types.Validation.UnitTests/Bicep.Types.Validation.UnitTests.csproj `
///   --filter "TestCategory=Baseline" `
///   -- 'TestRunParameters.Parameter(name="SetBaseLine", value="true")'
/// </code>
/// or set <c>BICEP_TYPES_VALIDATION_SET_BASELINE=true</c>. The samples source root is discovered by
/// walking up to the test project, or can be overridden with
/// <c>BICEP_TYPES_VALIDATION_SAMPLES_ROOT</c>. A stale/relocated build must be rebuilt after an
/// update so the embedded baselines are refreshed before the comparison tests run.
/// </remarks>
[TestClass]
public class ValidationSampleBaselineTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("Baseline")]
    public void Update_baselines_when_requested()
    {
        var runParameter = TestContext.Properties.Contains("SetBaseLine")
            ? TestContext.Properties["SetBaseLine"] as string
            : null;

        if (!ValidationSampleBaselineUpdater.IsUpdateRequested(runParameter))
        {
            Assert.Inconclusive(
                "Baseline update is opt-in. Pass -- 'TestRunParameters.Parameter(name=\"SetBaseLine\", value=\"true\")' " +
                $"or set {ValidationSampleBaselineUpdater.SetBaselineEnvVar}=true to update baselines.");
            return;
        }

        var samplesRoot = ValidationSampleBaselineUpdater.ResolveSamplesRoot();
        var summary = ValidationSampleBaselineUpdater.UpdateCorpus(samplesRoot);

        foreach (var written in summary.Written)
        {
            TestContext.WriteLine($"UPDATED  {written}");
        }

        foreach (var mismatch in summary.Mismatches)
        {
            TestContext.WriteLine($"MISMATCH {mismatch}");
        }

        summary.Mismatches.Should().BeEmpty(
            "multi-input scenarios must produce identical normalized results before a baseline is written.");
    }

    [TestMethod]
    public void Coverage_report_is_emitted_to_the_test_output_directory()
    {
        var markdown = ValidationSampleCoverageReport.ToMarkdown(ValidationSampleCoverageReport.Build());

        var reportPath = Path.Combine(AppContext.BaseDirectory, "validation-sample-coverage.md");
        File.WriteAllText(reportPath, markdown);

        TestContext.WriteLine($"Coverage report written to {reportPath}");
        File.Exists(reportPath).Should().BeTrue();
    }
}
