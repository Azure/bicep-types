// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.UnitTests.Graph;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Policy;

[TestClass]
public class ResourceScopePolicyValidatorTests
{
    // A reached type file whose single ResourceType at /0 carries the given scope fields and a
    // body that resolves to the StringType at /1.
    private static string Package(string scopeFieldsJson) =>
        "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
        "\"body\":{\"$ref\":\"#/1\"}," + scopeFieldsJson + "}," +
        "{\"$type\":\"StringType\"}]";

    private static System.Collections.Generic.IReadOnlyList<TypeValidationDiagnostic> Run(
        string scopeFieldsJson, TypePackageValidationMode mode)
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json", Package(scopeFieldsJson));
        return PolicyTestHelpers.RunPolicy(PolicyTestHelpers.ResourceIndex("types.json#/0"), fs, mode);
    }

    [TestMethod]
    public void Legacy_scope_type_reports_bcpvt022_in_canonical()
    {
        Run("\"scopeType\":0", TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.CanonicalFormViolation)
            .Which.JsonPointer.Should().Be("/0/scopeType");
    }

    [TestMethod]
    public void Legacy_scope_type_reports_bcpvt023_in_compatible()
    {
        Run("\"scopeType\":0", TypePackageValidationMode.CompatibleReader)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.CompatibilityFormUsed)
            .Which.Severity.Should().Be(TypeValidationDiagnosticSeverity.Warning);
    }

    [TestMethod]
    public void Legacy_read_only_scopes_reports_bcpvt022_in_canonical()
    {
        Run("\"readOnlyScopes\":4", TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.CanonicalFormViolation)
            .Which.JsonPointer.Should().Be("/0/readOnlyScopes");
    }

    [TestMethod]
    public void Legacy_nonzero_flags_reports_bcpvt023_in_compatible()
    {
        Run("\"flags\":1", TypePackageValidationMode.CompatibleReader)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.CompatibilityFormUsed)
            .Which.JsonPointer.Should().Be("/0/flags");
    }

    [TestMethod]
    public void Flags_zero_with_modern_pair_reports_per_field_policy_not_mixing()
    {
        // flags:0 is not an effective legacy value, so pairing it with the modern scopes is not a
        // mix: the field is still classified per-field (BCPVT022), not BCPVT024.
        Run("\"readableScopes\":8,\"writableScopes\":8,\"flags\":0", TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.CanonicalFormViolation)
            .Which.JsonPointer.Should().Be("/0/flags");
    }

    [TestMethod]
    public void Mixed_modern_and_legacy_scope_type_reports_single_bcpvt024_in_canonical()
    {
        Run("\"readableScopes\":8,\"writableScopes\":8,\"scopeType\":0", TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ResourceScopeFormMixed);
    }

    [TestMethod]
    public void Mixed_modern_and_nonzero_flags_reports_single_bcpvt024_in_compatible()
    {
        // BCPVT024 is emitted in both modes; mixing is never merely a compatibility warning.
        Run("\"readableScopes\":8,\"writableScopes\":8,\"flags\":2", TypePackageValidationMode.CompatibleReader)
            .Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ResourceScopeFormMixed);
    }

    [TestMethod]
    public void Mixed_scope_form_reports_single_bcpvt024_and_suppresses_per_field_policy_diagnostics()
    {
        var diagnostics = Run(
            "\"readableScopes\":8,\"writableScopes\":8,\"scopeType\":0,\"readOnlyScopes\":4",
            TypePackageValidationMode.CanonicalWriter);

        diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ResourceScopeFormMixed);
        // The single diagnostic names the first effective legacy field.
        diagnostics.Single().Message.Should().Contain("scopeType");
    }

    [TestMethod]
    public void Resource_scope_policy_reads_only_resource_type_root_flags()
    {
        // The ResourceType has no root-level legacy scope fields. A property-level 'flags' on a
        // reached ObjectType must not be read by the ResourceType scope policy.
        const string types =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":8,\"writableScopes\":8}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\"," +
            "\"properties\":{\"p\":{\"type\":{\"$ref\":\"#/2\"},\"flags\":1}}}," +
            "{\"$type\":\"StringType\"}]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);

        PolicyTestHelpers.RunPolicy(PolicyTestHelpers.ResourceIndex("types.json#/0"), fs,
            TypePackageValidationMode.CanonicalWriter)
            .Should().BeEmpty();
    }

    [TestMethod]
    public void Wrong_shape_legacy_field_is_owned_by_structural_layer()
    {
        // A non-integer legacy field is a structural primitive-shape error; policy stays silent.
        Run("\"scopeType\":\"all\"", TypePackageValidationMode.CanonicalWriter)
            .Should().BeEmpty();
    }
}
