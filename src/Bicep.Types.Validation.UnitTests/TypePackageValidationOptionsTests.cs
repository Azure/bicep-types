// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests;

[TestClass]
public class TypePackageValidationOptionsTests
{
    [TestMethod]
    public void Default_options_use_canonical_writer_mode()
        => new TypePackageValidationOptions().Mode.Should().Be(TypePackageValidationMode.CanonicalWriter);

    [TestMethod]
    public void Default_options_use_bicep_types_v1_format_version()
        => new TypePackageValidationOptions().FormatVersion.Should().Be(TypePackageFormatVersion.BicepTypesV1);

    [TestMethod]
    public void Bicep_types_v1_is_the_default_enum_value()
        => default(TypePackageFormatVersion).Should().Be(TypePackageFormatVersion.BicepTypesV1);

    [TestMethod]
    public void Default_options_include_warnings()
        => new TypePackageValidationOptions().IncludeWarnings.Should().BeTrue();

    [TestMethod]
    public void Default_options_exclude_informational_diagnostics()
        => new TypePackageValidationOptions().IncludeInformationalDiagnostics.Should().BeFalse();

    [TestMethod]
    public void Default_options_do_not_validate_unreachable_files()
        => new TypePackageValidationOptions().ValidateUnreachableFiles.Should().BeFalse();

    [TestMethod]
    public void Default_options_use_default_archive_limits()
    {
        var limits = new TypePackageValidationOptions().ArchiveLimits;

        limits.MaxCompressedArchiveBytes.Should().Be(64L * 1024L * 1024L);
        limits.MaxExpandedArchiveBytes.Should().Be(256L * 1024L * 1024L);
        limits.MaxPackageFileBytes.Should().Be(32L * 1024L * 1024L);
        limits.MaxPackageFileCount.Should().Be(4096);
    }

    [TestMethod]
    public void Archive_limits_can_be_overridden()
    {
        var limits = new TypePackageArchiveLimits(
            maxCompressedArchiveBytes: 1,
            maxExpandedArchiveBytes: 2,
            maxPackageFileBytes: 3,
            maxPackageFileCount: 4);

        limits.MaxCompressedArchiveBytes.Should().Be(1);
        limits.MaxExpandedArchiveBytes.Should().Be(2);
        limits.MaxPackageFileBytes.Should().Be(3);
        limits.MaxPackageFileCount.Should().Be(4);
    }

    [TestMethod]
    [DataRow(0L, 1L, 1L, 1)]
    [DataRow(1L, 0L, 1L, 1)]
    [DataRow(1L, 1L, 0L, 1)]
    [DataRow(1L, 1L, 1L, 0)]
    [DataRow(-1L, 1L, 1L, 1)]
    public void Archive_limits_reject_non_positive_values(
        long maxCompressedArchiveBytes,
        long maxExpandedArchiveBytes,
        long maxPackageFileBytes,
        int maxPackageFileCount)
    {
        Action act = () => new TypePackageArchiveLimits(
            maxCompressedArchiveBytes,
            maxExpandedArchiveBytes,
            maxPackageFileBytes,
            maxPackageFileCount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Archive_limits_cannot_be_null()
    {
        Action act = () => new TypePackageValidationOptions { ArchiveLimits = null! };

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Default_options_have_no_diagnostic_cap()
        => new TypePackageValidationOptions().MaxDiagnostics.Should().BeNull();

    [TestMethod]
    public void Max_diagnostics_accepts_null()
        => new TypePackageValidationOptions { MaxDiagnostics = null }.MaxDiagnostics.Should().BeNull();

    [TestMethod]
    public void Max_diagnostics_accepts_zero()
        => new TypePackageValidationOptions { MaxDiagnostics = 0 }.MaxDiagnostics.Should().Be(0);

    [TestMethod]
    public void Max_diagnostics_accepts_positive_values()
        => new TypePackageValidationOptions { MaxDiagnostics = 5 }.MaxDiagnostics.Should().Be(5);

    [TestMethod]
    public void Max_diagnostics_rejects_negative_values()
    {
        Action act = () => new TypePackageValidationOptions { MaxDiagnostics = -1 };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
