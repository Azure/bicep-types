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
    public void Default_options_include_warnings()
        => new TypePackageValidationOptions().IncludeWarnings.Should().BeTrue();

    [TestMethod]
    public void Default_options_exclude_informational_diagnostics()
        => new TypePackageValidationOptions().IncludeInformationalDiagnostics.Should().BeFalse();

    [TestMethod]
    public void Default_options_do_not_validate_unreachable_files()
        => new TypePackageValidationOptions().ValidateUnreachableFiles.Should().BeFalse();

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
