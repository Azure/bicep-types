// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

[TestClass]
public class ValidationSampleBaselineUpdaterTests
{
    [TestMethod]
    public void IsUpdateRequested_is_false_without_flag_or_env_var()
    {
        // The env var is not expected to be set during normal test runs.
        Environment.GetEnvironmentVariable(ValidationSampleBaselineUpdater.SetBaselineEnvVar)
            .Should().BeNullOrEmpty("normal test runs must not enable baseline updates");
        ValidationSampleBaselineUpdater.IsUpdateRequested(null).Should().BeFalse();
        ValidationSampleBaselineUpdater.IsUpdateRequested("false").Should().BeFalse();
    }

    [TestMethod]
    public void IsUpdateRequested_is_true_with_run_parameter()
        => ValidationSampleBaselineUpdater.IsUpdateRequested("true").Should().BeTrue();

    [TestMethod]
    public void ComputeWriteTarget_stays_under_samples_root()
    {
        using var root = new TempDir();

        var target = ValidationSampleBaselineUpdater.ComputeWriteTarget(
            root.Path, "Files/validation-samples/invalid/graph/x", "canonicalWriter");

        target.Should().StartWith(Path.GetFullPath(root.Path));
        target.Replace('\\', '/').Should().EndWith("invalid/graph/x/expected/canonicalWriter.result.json");
    }

    [TestMethod]
    public void ComputeWriteTarget_rejects_paths_that_escape_the_root()
    {
        using var root = new TempDir();

        Action act = () => ValidationSampleBaselineUpdater.ComputeWriteTarget(
            root.Path, "Files/validation-samples/../../evil", "canonicalWriter");

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void WriteBaselineIfChanged_creates_missing_file()
    {
        using var root = new TempDir();
        var path = Path.Combine(root.Path, "expected", "canonicalWriter.result.json");
        var content = ValidationSampleResultNormalizer.Canonicalize("{\"isValid\":true}");

        var written = ValidationSampleBaselineUpdater.WriteBaselineIfChanged(path, content);

        written.Should().BeTrue();
        File.Exists(path).Should().BeTrue();
    }

    [TestMethod]
    public void WriteBaselineIfChanged_does_not_rewrite_unchanged_content()
    {
        using var root = new TempDir();
        var path = Path.Combine(root.Path, "b.result.json");
        var content = ValidationSampleResultNormalizer.Canonicalize("{\"isValid\":true}");

        ValidationSampleBaselineUpdater.WriteBaselineIfChanged(path, content).Should().BeTrue();
        ValidationSampleBaselineUpdater.WriteBaselineIfChanged(path, content).Should().BeFalse();
    }

    [TestMethod]
    public void WriteBaselineIfChanged_rewrites_changed_content()
    {
        using var root = new TempDir();
        var path = Path.Combine(root.Path, "b.result.json");

        ValidationSampleBaselineUpdater.WriteBaselineIfChanged(
            path, ValidationSampleResultNormalizer.Canonicalize("{\"isValid\":true}")).Should().BeTrue();
        ValidationSampleBaselineUpdater.WriteBaselineIfChanged(
            path, ValidationSampleResultNormalizer.Canonicalize("{\"isValid\":false}")).Should().BeTrue();
    }

    [TestMethod]
    public void TryReconcileInputs_agrees_when_all_identical()
    {
        var results = new List<string> { "same", "same", "same" };

        ValidationSampleBaselineUpdater.TryReconcileInputs(results, out var agreed, out var index)
            .Should().BeTrue();
        agreed.Should().Be("same");
        index.Should().Be(-1);
    }

    [TestMethod]
    public void TryReconcileInputs_detects_first_divergent_input()
    {
        var results = new List<string> { "a", "a", "b" };

        ValidationSampleBaselineUpdater.TryReconcileInputs(results, out _, out var index)
            .Should().BeFalse();
        index.Should().Be(2);
    }

    [TestMethod]
    public void TryReconcileInputs_throws_on_empty()
    {
        Action act = () => ValidationSampleBaselineUpdater.TryReconcileInputs(
            new List<string>(), out _, out _);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void UpdateGroup_writes_exactly_once_when_all_inputs_agree()
    {
        using var root = new TempDir();
        var inputs = new List<ValidationSampleInput>
        {
            new(ValidationSampleInputKind.Directory, "package"),
            new(ValidationSampleInputKind.ArchiveFile, "package.tgz"),
        };
        var writeCount = 0;

        var outcome = ValidationSampleBaselineUpdater.UpdateGroup(
            root.Path,
            "Files/validation-samples/invalid/graph/x",
            "x",
            "canonicalWriter",
            inputs,
            _ => "IDENTICAL",
            (_, _) => { writeCount++; return true; });

        outcome.Kind.Should().Be(ValidationSampleBaselineUpdater.GroupUpdateKind.Written);
        outcome.RelativeTarget.Should().Be("invalid/graph/x/expected/canonicalWriter.result.json");
        writeCount.Should().Be(1, "an agreeing multi-input group must write exactly one baseline");
    }

    [TestMethod]
    public void UpdateGroup_reports_unchanged_when_writer_reports_no_change()
    {
        using var root = new TempDir();
        var inputs = new List<ValidationSampleInput> { new(ValidationSampleInputKind.Directory, "package") };

        var outcome = ValidationSampleBaselineUpdater.UpdateGroup(
            root.Path,
            "Files/validation-samples/invalid/graph/x",
            "x",
            "canonicalWriter",
            inputs,
            _ => "IDENTICAL",
            (_, _) => false);

        outcome.Kind.Should().Be(ValidationSampleBaselineUpdater.GroupUpdateKind.Unchanged);
        outcome.RelativeTarget.Should().Be("invalid/graph/x/expected/canonicalWriter.result.json");
    }

    [TestMethod]
    public void UpdateGroup_does_not_write_and_reports_mismatch_when_inputs_differ()
    {
        using var root = new TempDir();
        var inputs = new List<ValidationSampleInput>
        {
            new(ValidationSampleInputKind.Directory, "package"),
            new(ValidationSampleInputKind.ArchiveFile, "package.tgz"),
        };
        var writeCount = 0;

        var outcome = ValidationSampleBaselineUpdater.UpdateGroup(
            root.Path,
            "Files/validation-samples/invalid/graph/x",
            "x",
            "canonicalWriter",
            inputs,
            input => input.Path, // distinct content per input
            (_, _) => { writeCount++; return true; });

        outcome.Kind.Should().Be(ValidationSampleBaselineUpdater.GroupUpdateKind.Mismatch);
        writeCount.Should().Be(0, "a divergent multi-input group must not write any baseline");
        // The mismatch message must identify both the first and divergent input paths.
        outcome.MismatchMessage.Should().Contain("package.tgz");
        outcome.MismatchMessage.Should().Contain("first input 'Directory:package'");
    }

    [TestMethod]
    public void UpdateGroup_writes_one_file_through_real_writer_and_is_idempotent()
    {
        using var root = new TempDir();
        var inputs = new List<ValidationSampleInput>
        {
            new(ValidationSampleInputKind.Directory, "package"),
            new(ValidationSampleInputKind.ArchiveFile, "package.tgz"),
        };
        var content = ValidationSampleResultNormalizer.Canonicalize("{\"isValid\":true}");

        var first = ValidationSampleBaselineUpdater.UpdateGroup(
            root.Path, "Files/validation-samples/invalid/graph/x", "x", "canonicalWriter",
            inputs, _ => content, ValidationSampleBaselineUpdater.WriteBaselineIfChanged);

        var writtenPath = Path.Combine(root.Path, "invalid", "graph", "x", "expected", "canonicalWriter.result.json");
        first.Kind.Should().Be(ValidationSampleBaselineUpdater.GroupUpdateKind.Written);
        File.Exists(writtenPath).Should().BeTrue();

        // Second run with identical content must be a no-op.
        var second = ValidationSampleBaselineUpdater.UpdateGroup(
            root.Path, "Files/validation-samples/invalid/graph/x", "x", "canonicalWriter",
            inputs, _ => content, ValidationSampleBaselineUpdater.WriteBaselineIfChanged);

        second.Kind.Should().Be(ValidationSampleBaselineUpdater.GroupUpdateKind.Unchanged);
    }

    [TestMethod]
    public void ResolveSamplesRoot_prefers_explicit_existing_root()
    {
        using var root = new TempDir();

        ValidationSampleBaselineUpdater.ResolveSamplesRoot(root.Path)
            .Should().Be(Path.GetFullPath(root.Path));
    }

    [TestMethod]
    public void ResolveSamplesRoot_throws_for_missing_explicit_root()
    {
        Action act = () => ValidationSampleBaselineUpdater.ResolveSamplesRoot(
            Path.Combine(Path.GetTempPath(), "definitely-missing-" + Guid.NewGuid().ToString("N")));

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [TestMethod]
    public void ResolveSamplesRoot_locates_source_tree_by_walking_up()
    {
        var root = ValidationSampleBaselineUpdater.ResolveSamplesRoot();

        Directory.Exists(root).Should().BeTrue();
        root.Replace('\\', '/').Should().EndWith("Files/validation-samples");
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bcpvt-baseline-test-" + System.IO.Path.GetRandomFileName());

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best-effort */ }
        }
    }
}

[TestClass]
public class ValidationSampleCoverageReportTests
{
    [TestMethod]
    public void Report_is_deterministic()
    {
        var first = ValidationSampleCoverageReport.ToMarkdown(ValidationSampleCoverageReport.Build());
        var second = ValidationSampleCoverageReport.ToMarkdown(ValidationSampleCoverageReport.Build());

        second.Should().Be(first);
    }

    [TestMethod]
    public void Report_contains_expected_sections()
    {
        var markdown = ValidationSampleCoverageReport.ToMarkdown(ValidationSampleCoverageReport.Build());

        markdown.Should().Contain("# Validation Sample Coverage");
        markdown.Should().Contain("## By Diagnostic Code");
        markdown.Should().Contain("## By Category");
        markdown.Should().Contain("## By Mode");
        markdown.Should().Contain("## By Input Kind");
        markdown.Should().Contain("## Corpus Gaps");
    }

    [TestMethod]
    public void Report_covers_known_diagnostic_codes_from_baselines()
    {
        var model = ValidationSampleCoverageReport.Build();

        model.DiagnosticRows.Should().NotBeEmpty();
        // Every known category should appear (no missing-category gap for the current corpus).
        model.Gaps.Should().NotContain(g => g.Contains("has no scenarios"));
    }

    [TestMethod]
    public void Report_does_not_apply_category_based_validity_gaps()
    {
        var model = ValidationSampleCoverageReport.Build();

        // The report must not infer expected diagnostics from a scenario's category. Gaps are limited
        // to coverage-presence checks (a known category with no scenarios); per-scenario validity is
        // owned by the baseline-internal invariants in the health tests. For the current corpus every
        // known category is present, so there are no gaps at all.
        model.Gaps.Should().NotContain(g => g.Contains("error diagnostic"));
        foreach (var gap in model.Gaps)
        {
            gap.Should().Contain("has no scenarios", "the only coverage gaps are missing-category presence checks");
        }

        // Intentional compatibility scenarios (error in canonicalWriter, warning in compatibleReader)
        // must never be reported as gaps.
        foreach (var name in new[]
        {
            "object-property-flags-invalid",
            "readable-scope-bits-invalid",
            "visible-in-file-kind-invalid",
        })
        {
            model.Gaps.Should().NotContain(g => g.Contains(name));
        }
    }
}
