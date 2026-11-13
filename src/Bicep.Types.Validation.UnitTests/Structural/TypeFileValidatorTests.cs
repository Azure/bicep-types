// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.Structural;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Structural;

[TestClass]
public class TypeFileValidatorTests
{
    private static (StructuralValidationContext ctx, JsonShapeReader reader) CreateContext(
        string typeFileJson,
        TypePackageValidationMode mode = TypePackageValidationMode.CanonicalWriter)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(typeFileJson);
        SourceMap.TryParse(bytes, "types.json", out var root, out var sm, out _);
        var doc = new PackageDocument("types.json", PackageDocumentKind.TypeFile, root!, sm);
        var options = new TypePackageValidationOptions { Mode = mode };
        var ctx = new StructuralValidationContext(options);
        ctx.SetCurrentDocument(doc);
        return (ctx, new JsonShapeReader(ctx));
    }

    // ── Root shape ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Non_array_root_reports_type_file_root_must_be_array()
    {
        var (ctx, reader) = CreateContext("{\"bad\":1}");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeFileRootMustBeArray);
    }

    // ── Element shape ────────────────────────────────────────────────────────

    [TestMethod]
    public void Primitive_array_element_reports_type_file_element_must_be_object()
    {
        var (ctx, reader) = CreateContext("[42]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeFileElementMustBeObject);
    }

    [TestMethod]
    public void Null_array_element_reports_type_file_element_must_be_object()
    {
        var (ctx, reader) = CreateContext("[null]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeFileElementMustBeObject);
    }

    // ── $type discriminator ──────────────────────────────────────────────────

    [TestMethod]
    public void Type_object_without_dollar_type_reports_discriminator_missing()
    {
        var (ctx, reader) = CreateContext("[{\"name\":\"x\"}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeObjectDiscriminatorMissing);
    }

    [TestMethod]
    public void Non_string_dollar_type_reports_discriminator_must_be_string()
    {
        var (ctx, reader) = CreateContext("[{\"$type\":42}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeObjectDiscriminatorMustBeString);
    }

    [TestMethod]
    public void Unknown_dollar_type_reports_discriminator_unsupported()
    {
        var (ctx, reader) = CreateContext("[{\"$type\":\"NonExistentKind\"}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeObjectDiscriminatorUnsupported);
    }

    // ── Field validation ─────────────────────────────────────────────────────

    [TestMethod]
    public void Missing_required_field_reports_required_property_missing()
    {
        // StringLiteralType requires "value" field
        var (ctx, reader) = CreateContext("[{\"$type\":\"StringLiteralType\"}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.RequiredPropertyMissing && d.Message.Contains("value"));
    }

    [TestMethod]
    public void Wrong_primitive_field_shape_reports_property_type_mismatch()
    {
        // StringLiteralType.value must be a string
        var (ctx, reader) = CreateContext("[{\"$type\":\"StringLiteralType\",\"value\":42}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.PropertyTypeMismatch);
    }

    [TestMethod]
    public void Unknown_type_object_field_reports_unknown_property_in_canonical_mode()
    {
        var (ctx, reader) = CreateContext("[{\"$type\":\"AnyType\",\"unknownField\":1}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnknownProperty);
    }

    [TestMethod]
    public void Legacy_field_is_structurally_known_in_canonical_mode()
    {
        // scopeType is a documented legacy field. As of phase 4 the structural layer treats it as
        // known in both modes; the mode-policy layer owns rejecting it in CanonicalWriter.
        var json = "[{\"$type\":\"ResourceType\",\"name\":\"X\",\"body\":{\"$ref\":\"#/0\"}," +
                   "\"readableScopes\":8,\"writableScopes\":8,\"scopeType\":0}]";
        var (ctx, reader) = CreateContext(json, TypePackageValidationMode.CanonicalWriter);
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    [TestMethod]
    public void Legacy_field_is_accepted_in_compatible_reader_mode()
    {
        // The same documented legacy field is accepted in CompatibleReader.
        var json = "[{\"$type\":\"ResourceType\",\"name\":\"X\",\"body\":{\"$ref\":\"#/0\"}," +
                   "\"readableScopes\":8,\"writableScopes\":8,\"scopeType\":0}]";
        var (ctx, reader) = CreateContext(json, TypePackageValidationMode.CompatibleReader);
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    [TestMethod]
    public void Arbitrary_unknown_field_is_rejected_in_compatible_reader_mode()
    {
        // CompatibleReader accepts only documented legacy fields, not arbitrary unknowns.
        var (ctx, reader) = CreateContext(
            "[{\"$type\":\"AnyType\",\"unknownField\":1}]", TypePackageValidationMode.CompatibleReader);
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnknownProperty);
    }

    [TestMethod]
    public void Non_integer_number_in_integer_field_reports_property_type_mismatch()
    {
        // IntegerType.minValue is an integer field; a fractional number must be rejected.
        var (ctx, reader) = CreateContext("[{\"$type\":\"IntegerType\",\"minValue\":1.5}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.PropertyTypeMismatch);
    }

    [TestMethod]
    public void Malformed_reference_field_reports_reference_object_invalid()
    {
        // ArrayType.itemType must be a ref, but we're giving a string
        var (ctx, reader) = CreateContext("[{\"$type\":\"ArrayType\",\"itemType\":\"not-a-ref\"}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceObjectInvalid);
    }

    // ── Valid minimal type objects ───────────────────────────────────────────

    [TestMethod]
    public void Any_type_object_with_no_extra_fields_is_valid()
    {
        var (ctx, reader) = CreateContext("[{\"$type\":\"AnyType\"}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    [TestMethod]
    public void String_literal_type_with_value_is_valid()
    {
        var (ctx, reader) = CreateContext("[{\"$type\":\"StringLiteralType\",\"value\":\"hello\"}]");
        TypeFileValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }
}
