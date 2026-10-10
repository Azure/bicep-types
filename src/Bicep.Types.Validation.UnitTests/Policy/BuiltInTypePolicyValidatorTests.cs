// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.UnitTests.Graph;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Policy;

[TestClass]
public class BuiltInTypePolicyValidatorTests
{
    // A reached type file: /0 ResourceType (modern scopes only) whose body reaches the
    // BuiltInType under test at /1.
    private static string Package(string builtInKindJson) =>
        "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
        "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":8,\"writableScopes\":8}," +
        "{\"$type\":\"BuiltInType\"," + builtInKindJson + "}]";

    private static System.Collections.Generic.IReadOnlyList<TypeValidationDiagnostic> Run(
        string builtInKindJson, TypePackageValidationMode mode)
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json", Package(builtInKindJson));
        return PolicyTestHelpers.RunPolicy(PolicyTestHelpers.ResourceIndex("types.json#/0"), fs, mode);
    }

    [TestMethod]
    public void Documented_builtin_kind_reports_bcpvt022_in_canonical()
    {
        Run("\"kind\":5", TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.CanonicalFormViolation)
            .Which.JsonPointer.Should().Be("/1/kind");
    }

    [TestMethod]
    public void Documented_builtin_kind_reports_bcpvt023_in_compatible()
    {
        var diagnostics = Run("\"kind\":5", TypePackageValidationMode.CompatibleReader);

        diagnostics.Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.CompatibilityFormUsed)
            .Which.Severity.Should().Be(TypeValidationDiagnosticSeverity.Warning);
    }

    [TestMethod]
    public void Reserved_builtin_kind_8_reports_bcpvt022_in_canonical()
    {
        // Kind 8 (ResourceRef) has no canonical replacement but is still rejected in canonical.
        Run("\"kind\":8", TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.CanonicalFormViolation);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(9)]
    [DataRow(-1)]
    public void Out_of_range_builtin_kind_reports_bcpvt025_in_canonical(int kind)
    {
        Run("\"kind\":" + kind, TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.BuiltInTypeKindInvalid);
    }

    [TestMethod]
    public void Out_of_range_builtin_kind_reports_bcpvt025_in_compatible()
    {
        Run("\"kind\":42", TypePackageValidationMode.CompatibleReader)
            .Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.BuiltInTypeKindInvalid);
    }

    [TestMethod]
    public void Missing_builtin_kind_is_owned_by_structural_layer()
    {
        // No 'kind' property: the structural layer owns the required-property diagnostic; policy is silent.
        Run("\"name\":\"unused\"", TypePackageValidationMode.CanonicalWriter)
            .Should().BeEmpty();
    }

    [TestMethod]
    public void Non_integer_builtin_kind_is_owned_by_structural_layer()
    {
        Run("\"kind\":\"five\"", TypePackageValidationMode.CanonicalWriter)
            .Should().BeEmpty();
    }
}
