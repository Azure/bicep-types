// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Azure.Bicep.Types.Validation.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests;

[TestClass]
public class TypePackageValidatorTests
{
    private static readonly TypePackageValidator Validator = new();

    [TestMethod]
    public void Directory_input_returns_empty_valid_result()
    {
        var result = Validator.Validate(TypePackageValidationInput.ForDirectory("some/package"));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
        result.DiagnosticsTruncated.Should().BeFalse();
        result.Summary.ErrorCount.Should().Be(0);
    }

    [TestMethod]
    public void Index_file_input_returns_empty_valid_result()
    {
        var result = Validator.Validate(TypePackageValidationInput.ForIndexFile("some/package/index.json"));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Archive_file_input_returns_not_implemented_error()
    {
        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("some/types.tgz"));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveValidationNotImplemented);
        result.Summary.ErrorCount.Should().Be(1);
    }

    [TestMethod]
    public void Archive_stream_input_returns_not_implemented_error()
    {
        using var stream = new MemoryStream();

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveValidationNotImplemented);
    }

    [TestMethod]
    public void Result_echoes_selected_mode()
    {
        var options = new TypePackageValidationOptions { Mode = TypePackageValidationMode.CompatibleReader };

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory("some/package"), options);

        result.Mode.Should().Be(TypePackageValidationMode.CompatibleReader);
    }

    [TestMethod]
    public void Default_mode_is_canonical_writer()
    {
        var result = Validator.Validate(TypePackageValidationInput.ForDirectory("some/package"));

        result.Mode.Should().Be(TypePackageValidationMode.CanonicalWriter);
    }

    [TestMethod]
    public void Null_input_throws()
    {
        Action act = () => Validator.Validate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Null_max_diagnostics_leaves_truncation_false_for_archive_error()
    {
        var options = new TypePackageValidationOptions { MaxDiagnostics = null };

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("types.tgz"), options);

        result.DiagnosticsTruncated.Should().BeFalse();
    }

    [TestMethod]
    public void Positive_max_diagnostics_not_exceeded_leaves_truncation_false()
    {
        var options = new TypePackageValidationOptions { MaxDiagnostics = 1 };

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("types.tgz"), options);

        result.Diagnostics.Should().ContainSingle();
        result.DiagnosticsTruncated.Should().BeFalse();
    }
}
