// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Diagnostics;

[TestClass]
public class TypePackageValidationResultTests
{
    [TestMethod]
    public void Result_is_valid_when_there_are_no_error_diagnostics()
    {
        var result = Create(new[] { Warning() }, new TypePackageValidationOptions());

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void Result_is_invalid_when_an_error_diagnostic_exists()
    {
        var result = Create(new[] { Error() }, new TypePackageValidationOptions());

        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void Summary_counts_all_detected_diagnostics_before_filtering_and_truncation()
    {
        var options = new TypePackageValidationOptions
        {
            IncludeWarnings = false,
            IncludeInformationalDiagnostics = false,
            MaxDiagnostics = 1,
        };

        var result = Create(new[] { Error(), Warning(), Information() }, options);

        result.Summary.ErrorCount.Should().Be(1);
        result.Summary.WarningCount.Should().Be(1);
        result.Summary.InfoCount.Should().Be(1);
    }

    [TestMethod]
    public void IsValid_reflects_detected_errors_even_when_filtered_or_truncated()
    {
        var options = new TypePackageValidationOptions { MaxDiagnostics = 1 };

        var result = Create(new[] { Error(), Warning(), Warning() }, options);

        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void Warning_filtering_affects_only_returned_diagnostics()
    {
        var options = new TypePackageValidationOptions { IncludeWarnings = false };

        var result = Create(new[] { Warning(), Error() }, options);

        result.Diagnostics.Should().OnlyContain(d => d.Severity != TypeValidationDiagnosticSeverity.Warning);
        result.Summary.WarningCount.Should().Be(1);
    }

    [TestMethod]
    public void Informational_diagnostics_are_excluded_by_default_but_counted()
    {
        var result = Create(new[] { Information() }, new TypePackageValidationOptions());

        result.Diagnostics.Should().BeEmpty();
        result.Summary.InfoCount.Should().Be(1);
    }

    [TestMethod]
    public void Informational_diagnostics_are_returned_when_requested()
    {
        var options = new TypePackageValidationOptions { IncludeInformationalDiagnostics = true };

        var result = Create(new[] { Information() }, options);

        result.Diagnostics.Should().ContainSingle();
    }

    [TestMethod]
    public void Null_max_diagnostics_applies_no_cap()
    {
        var options = new TypePackageValidationOptions { MaxDiagnostics = null };

        var result = Create(new[] { Error("BCPVT100", "a.json"), Error("BCPVT101", "b.json"), Error("BCPVT102", "c.json") }, options);

        result.DiagnosticsTruncated.Should().BeFalse();
        result.Diagnostics.Should().HaveCount(3);
    }

    [TestMethod]
    public void Positive_max_diagnostics_truncates_and_sets_flag()
    {
        var options = new TypePackageValidationOptions { MaxDiagnostics = 2 };

        var result = Create(new[] { Error("BCPVT100", "a.json"), Error("BCPVT101", "b.json"), Error("BCPVT102", "c.json") }, options);

        result.DiagnosticsTruncated.Should().BeTrue();
        result.Diagnostics.Should().HaveCount(2);
        result.Summary.ErrorCount.Should().Be(3);
    }

    [TestMethod]
    public void Max_diagnostics_not_exceeded_leaves_truncation_false()
    {
        var options = new TypePackageValidationOptions { MaxDiagnostics = 5 };

        var result = Create(new[] { Error("BCPVT100", "a.json"), Error("BCPVT101", "b.json") }, options);

        result.DiagnosticsTruncated.Should().BeFalse();
        result.Diagnostics.Should().HaveCount(2);
    }

    [TestMethod]
    public void Returned_diagnostics_are_sorted()
    {
        var later = new TypeValidationDiagnostic("BCPVT200", TypeValidationDiagnosticSeverity.Error, "b", path: "types.json", line: 2, column: 1);
        var earlier = new TypeValidationDiagnostic("BCPVT100", TypeValidationDiagnosticSeverity.Error, "a", path: "index.json", line: 1, column: 1);

        var result = Create(new[] { later, earlier }, new TypePackageValidationOptions());

        result.Diagnostics[0].Should().BeSameAs(earlier);
        result.Diagnostics[1].Should().BeSameAs(later);
    }

    private static TypePackageValidationResult Create(
        IEnumerable<TypeValidationDiagnostic> diagnostics,
        TypePackageValidationOptions options)
        => TypePackageValidationResult.Create(TypePackageValidationMode.CanonicalWriter, diagnostics, options);

    private static TypeValidationDiagnostic Error(string code = "BCPVT001", string? path = null)
        => new(code, TypeValidationDiagnosticSeverity.Error, "error", path: path);

    private static TypeValidationDiagnostic Warning(string code = "BCPVT500", string? path = null)
        => new(code, TypeValidationDiagnosticSeverity.Warning, "warning", path: path);

    private static TypeValidationDiagnostic Information(string code = "BCPVT900", string? path = null)
        => new(code, TypeValidationDiagnosticSeverity.Info, "info", path: path);
}
