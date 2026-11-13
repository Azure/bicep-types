// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Semantic;
using Azure.Bicep.Types.Validation.UnitTests.Graph;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Semantic;

[TestClass]
public class ScalarSemanticValidatorTests
{
    // A resource whose body targets /1, with in-mask modern scopes, followed by an empty object
    // at /1. The reached type file is scanned in full, so a test type placed at /2 is validated
    // even though nothing references it.
    private const string ResourcePrefix =
        "{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
        "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":8,\"writableScopes\":8},";

    private const string EmptyObject =
        "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}";

    private static string TypesWithTail(string tailType) =>
        "[" + ResourcePrefix + EmptyObject + "," + tailType + "]";

    private static IReadOnlyList<TypeValidationDiagnostic> Run(string typesJson, TypePackageValidationMode mode)
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json", typesJson);
        var options = new TypePackageValidationOptions { Mode = mode };
        var index = GraphTestHelpers.Document("index.json", ResourceIndex("types.json#/0"));
        var provider = new PackageDocumentProvider(fs, index, options);

        // Trigger graph traversal so the provider caches the reached type file.
        SemanticGraphValidator.Validate(provider, index, options);

        return ScalarSemanticValidator.Validate(provider.GetReachedUsableTypeFiles(), options);
    }

    private static string ResourceIndex(string refValue) =>
        "{\"resources\":{\"My.Rp/x@2026-01-01\":{\"$ref\":\"" + refValue + "\"}}," +
        "\"resourceFunctions\":{},\"namespaceFunctions\":[]}";

    // ── BCPVT026: numeric range ordering ─────────────────────────────────────

    [TestMethod]
    public void Integer_min_greater_than_max_reports_bcpvt026()
    {
        var types = TypesWithTail("{\"$type\":\"IntegerType\",\"minValue\":10,\"maxValue\":5}");

        var diagnostic = Run(types, TypePackageValidationMode.CanonicalWriter).Should().ContainSingle().Subject;

        diagnostic.Code.Should().Be(TypeValidationDiagnosticCodes.NumericRangeInvalid);
        diagnostic.Severity.Should().Be(TypeValidationDiagnosticSeverity.Error);
        diagnostic.JsonPointer.Should().Be("/2");
        diagnostic.Message.Should().Contain("minValue 10 greater than maxValue 5");
    }

    [TestMethod]
    public void String_min_length_greater_than_max_length_reports_bcpvt026()
    {
        var types = TypesWithTail("{\"$type\":\"StringType\",\"minLength\":10,\"maxLength\":5}");

        Run(types, TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.NumericRangeInvalid)
            .Which.Message.Should().Contain("minLength 10 greater than maxLength 5");
    }

    [TestMethod]
    public void Array_min_length_greater_than_max_length_reports_bcpvt026()
    {
        var types = TypesWithTail(
            "{\"$type\":\"ArrayType\",\"itemType\":{\"$ref\":\"#/1\"},\"minLength\":10,\"maxLength\":5}");

        Run(types, TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.NumericRangeInvalid)
            .Which.JsonPointer.Should().Be("/2");
    }

    // ── BCPVT027: non-negative length ────────────────────────────────────────

    [TestMethod]
    public void Negative_string_length_reports_bcpvt027()
    {
        var types = TypesWithTail("{\"$type\":\"StringType\",\"minLength\":-1}");

        var diagnostic = Run(types, TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle().Subject;

        diagnostic.Code.Should().Be(TypeValidationDiagnosticCodes.LengthConstraintNegative);
        diagnostic.JsonPointer.Should().Be("/2/minLength");
        diagnostic.Message.Should().Contain("must be non-negative, but got -1");
    }

    [TestMethod]
    public void Negative_array_length_reports_bcpvt027()
    {
        var types = TypesWithTail(
            "{\"$type\":\"ArrayType\",\"itemType\":{\"$ref\":\"#/1\"},\"maxLength\":-1}");

        Run(types, TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.LengthConstraintNegative)
            .Which.JsonPointer.Should().Be("/2/maxLength");
    }

    // ── BCPVT029: scope flag domains ─────────────────────────────────────────

    [TestMethod]
    public void Readable_scope_with_unknown_bits_reports_bcpvt029_error_in_canonical_and_warning_in_compatible()
    {
        const string types =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":32,\"writableScopes\":8}," +
            EmptyObject + "]";

        var canonical = Run(types, TypePackageValidationMode.CanonicalWriter).Should().ContainSingle().Subject;
        canonical.Code.Should().Be(TypeValidationDiagnosticCodes.FlagsValueInvalid);
        canonical.Severity.Should().Be(TypeValidationDiagnosticSeverity.Error);
        canonical.JsonPointer.Should().Be("/0/readableScopes");
        canonical.Message.Should().Contain("unknown bits 32");
        canonical.Message.Should().Contain("Known mask is 31");

        Run(types, TypePackageValidationMode.CompatibleReader).Should().ContainSingle()
            .Which.Severity.Should().Be(TypeValidationDiagnosticSeverity.Warning);
    }

    [TestMethod]
    public void Writable_scope_with_unknown_bits_reports_bcpvt029_error_in_canonical_and_warning_in_compatible()
    {
        const string types =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":8,\"writableScopes\":64}," +
            EmptyObject + "]";

        Run(types, TypePackageValidationMode.CanonicalWriter).Should().ContainSingle()
            .Which.JsonPointer.Should().Be("/0/writableScopes");

        Run(types, TypePackageValidationMode.CompatibleReader).Should().ContainSingle()
            .Which.Severity.Should().Be(TypeValidationDiagnosticSeverity.Warning);
    }

    [TestMethod]
    public void Legacy_resource_scope_fields_are_skipped_by_scalar_domain_validation()
    {
        // scopeType is a legacy direct-root field owned by phase-4 policy; scalar domain
        // validation must never read it, so no BCPVT029 is produced even for out-of-mask bits.
        const string types =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"scopeType\":64}," +
            EmptyObject + "]";

        Run(types, TypePackageValidationMode.CanonicalWriter).Should().BeEmpty();
        Run(types, TypePackageValidationMode.CompatibleReader).Should().BeEmpty();
    }

    // ── BCPVT029: object/parameter flag domains ──────────────────────────────

    [TestMethod]
    public void Object_property_flags_with_unknown_bits_reports_bcpvt029_error_in_canonical_and_warning_in_compatible()
    {
        const string types =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":8,\"writableScopes\":8}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{" +
            "\"name\":{\"type\":{\"$ref\":\"#/1\"},\"flags\":32}}}]";

        var canonical = Run(types, TypePackageValidationMode.CanonicalWriter).Should().ContainSingle().Subject;
        canonical.Code.Should().Be(TypeValidationDiagnosticCodes.FlagsValueInvalid);
        canonical.Severity.Should().Be(TypeValidationDiagnosticSeverity.Error);
        canonical.JsonPointer.Should().Be("/1/properties/name/flags");

        Run(types, TypePackageValidationMode.CompatibleReader).Should().ContainSingle()
            .Which.Severity.Should().Be(TypeValidationDiagnosticSeverity.Warning);
    }

    [TestMethod]
    public void Namespace_function_parameter_flags_with_unknown_bits_reports_bcpvt029_error_in_canonical_and_warning_in_compatible()
    {
        var types = TypesWithTail(
            "{\"$type\":\"NamespaceFunctionType\",\"name\":\"f\"," +
            "\"parameters\":[{\"name\":\"p\",\"type\":{\"$ref\":\"#/1\"},\"flags\":8}]," +
            "\"outputType\":{\"$ref\":\"#/1\"},\"visibleInFileKind\":1}");

        var canonical = Run(types, TypePackageValidationMode.CanonicalWriter).Should().ContainSingle().Subject;
        canonical.Code.Should().Be(TypeValidationDiagnosticCodes.FlagsValueInvalid);
        canonical.JsonPointer.Should().Be("/2/parameters/0/flags");

        Run(types, TypePackageValidationMode.CompatibleReader).Should().ContainSingle()
            .Which.Severity.Should().Be(TypeValidationDiagnosticSeverity.Warning);
    }

    // ── BCPVT028: enum membership ────────────────────────────────────────────

    [TestMethod]
    public void Visible_in_file_kind_unknown_value_reports_bcpvt028_error_in_canonical_and_warning_in_compatible()
    {
        var types = TypesWithTail(
            "{\"$type\":\"NamespaceFunctionType\",\"name\":\"f\",\"parameters\":[]," +
            "\"outputType\":{\"$ref\":\"#/1\"},\"visibleInFileKind\":99}");

        var canonical = Run(types, TypePackageValidationMode.CanonicalWriter).Should().ContainSingle().Subject;
        canonical.Code.Should().Be(TypeValidationDiagnosticCodes.EnumValueInvalid);
        canonical.Severity.Should().Be(TypeValidationDiagnosticSeverity.Error);
        canonical.JsonPointer.Should().Be("/2/visibleInFileKind");
        canonical.Message.Should().Contain("must be one of 1 or 2");

        Run(types, TypePackageValidationMode.CompatibleReader).Should().ContainSingle()
            .Which.Severity.Should().Be(TypeValidationDiagnosticSeverity.Warning);
    }

    [TestMethod]
    public void Enum_domain_validation_uses_exact_membership_not_flags_mask()
    {
        // 3 == 1 | 2 as bits, but BicepSourceFileKind is a non-flags enum: only exact members
        // 1 and 2 are valid, so 3 must still be reported.
        var types = TypesWithTail(
            "{\"$type\":\"NamespaceFunctionType\",\"name\":\"f\",\"parameters\":[]," +
            "\"outputType\":{\"$ref\":\"#/1\"},\"visibleInFileKind\":3}");

        Run(types, TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.EnumValueInvalid);
    }

    [TestMethod]
    public void Flags_domain_validation_uses_mask_containment()
    {
        // 31 is the full ObjectTypePropertyFlags mask: every bit is known, so no diagnostic.
        const string inMask =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":8,\"writableScopes\":8}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{" +
            "\"name\":{\"type\":{\"$ref\":\"#/1\"},\"flags\":31}}}]";

        Run(inMask, TypePackageValidationMode.CanonicalWriter).Should().BeEmpty();

        // Adding a single out-of-mask bit (32) is reported.
        const string outOfMask =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":8,\"writableScopes\":8}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{" +
            "\"name\":{\"type\":{\"$ref\":\"#/1\"},\"flags\":63}}}]";

        Run(outOfMask, TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.FlagsValueInvalid)
            .Which.Message.Should().Contain("unknown bits 32");
    }

    // ── Structural ownership ─────────────────────────────────────────────────

    [TestMethod]
    public void Scalar_semantic_validator_skips_wrong_shape_fields_owned_by_structural_layer()
    {
        // minValue has the wrong primitive shape (string). The structural layer owns that
        // diagnostic; the scalar layer must not read the field or emit a range diagnostic.
        var types = TypesWithTail("{\"$type\":\"IntegerType\",\"minValue\":\"10\",\"maxValue\":5}");

        Run(types, TypePackageValidationMode.CanonicalWriter)
            .Should().NotContain(d => d.Code == TypeValidationDiagnosticCodes.NumericRangeInvalid);
    }
}
