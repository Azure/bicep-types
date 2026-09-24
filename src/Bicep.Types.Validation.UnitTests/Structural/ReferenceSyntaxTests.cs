// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.Structural;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Structural;

[TestClass]
public class ReferenceSyntaxTests
{
    // Helper: build a fake context pointing at a package document made from in-memory JSON
    private static (StructuralValidationContext ctx, JsonValueNode root) ParseJson(
        string json,
        PackageDocumentKind documentKind = PackageDocumentKind.Index,
        TypePackageValidationMode mode = TypePackageValidationMode.CanonicalWriter)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        string path = documentKind == PackageDocumentKind.Index ? "index.json" : "types.json";
        SourceMap.TryParse(bytes, path, out var root, out var sm, out _);
        var doc = new PackageDocument(path, documentKind, root!, sm);
        var ctx = new StructuralValidationContext(new TypePackageValidationOptions { Mode = mode });
        ctx.SetCurrentDocument(doc);
        return (ctx, root!);
    }

    private static JsonValueNode ParseRef(string refJson,
        StructuralValidationContext ctx) =>
        ParseJson(refJson).root;

    // ── Valid reference forms ────────────────────────────────────────────────

    [TestMethod]
    public void Same_file_ref_is_accepted()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"#/0\"}", PackageDocumentKind.TypeFile);
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeTrue();
        result.PackageRelativePath.Should().BeEmpty();
        result.Index.Should().Be(0);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    [TestMethod]
    public void Cross_file_ref_is_accepted()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"types.json#/0\"}", PackageDocumentKind.Index);
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeTrue();
        result.PackageRelativePath.Should().Be("types.json");
        result.Index.Should().Be(0);
    }

    [TestMethod]
    public void Nested_package_path_ref_is_accepted()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"common/types.json#/3\"}", PackageDocumentKind.Index);
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeTrue();
        result.PackageRelativePath.Should().Be("common/types.json");
        result.Index.Should().Be(3);
    }

    [TestMethod]
    [DataRow("./types.json#/0", "types.json")]
    [DataRow("common/./types.json#/0", "common/types.json")]
    [DataRow("common////types.json#/0", "common/types.json")]
    [DataRow(@"common\types.json#/0", "common/types.json")]
    public void Safe_reference_path_aliases_are_canonicalized(string reference, string expectedPath)
    {
        var (ctx, root) = ParseJson(
            $"{{\"$ref\":\"{reference.Replace("\\", "\\\\", System.StringComparison.Ordinal)}\"}}",
            PackageDocumentKind.Index);

        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);

        result.IsValid.Should().BeTrue();
        result.PackageRelativePath.Should().Be(expectedPath);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    [TestMethod]
    [DataRow(TypePackageValidationMode.CanonicalWriter)]
    [DataRow(TypePackageValidationMode.CompatibleReader)]
    public void Same_file_ref_in_index_is_rejected(TypePackageValidationMode mode)
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"#/0\"}", PackageDocumentKind.Index, mode);

        var result = ReferenceSyntax.Validate(root, "resource", "/resources/r", ctx);

        result.IsValid.Should().BeFalse();
        var diagnostic = ctx.GetDiagnostics().Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
        diagnostic.Path.Should().Be("index.json");
        diagnostic.JsonPointer.Should().Be("/resources/r/$ref");
        diagnostic.Line.Should().NotBeNull();
        diagnostic.Column.Should().NotBeNull();
        diagnostic.Message.Should().Contain("#/0");
        diagnostic.Message.Should().Contain("cross-file");
    }

    [TestMethod]
    [DataRow(TypePackageValidationMode.CanonicalWriter)]
    [DataRow(TypePackageValidationMode.CompatibleReader)]
    public void Cross_file_ref_in_type_file_is_rejected(TypePackageValidationMode mode)
    {
        var (ctx, root) = ParseJson(
            "{\"$ref\":\"./common/types.json#/3\"}",
            PackageDocumentKind.TypeFile,
            mode);

        var result = ReferenceSyntax.Validate(root, "body", "/0/body", ctx);

        result.IsValid.Should().BeFalse();
        var diagnostic = ctx.GetDiagnostics().Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
        diagnostic.Path.Should().Be("types.json");
        diagnostic.JsonPointer.Should().Be("/0/body/$ref");
        diagnostic.Line.Should().NotBeNull();
        diagnostic.Column.Should().NotBeNull();
        diagnostic.Message.Should().Contain("./common/types.json#/3");
        diagnostic.Message.Should().Contain("same-file");
    }

    // ── Invalid reference forms ──────────────────────────────────────────────

    [TestMethod]
    public void Empty_ref_string_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    public void Missing_fragment_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"types.json\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    public void Non_integer_index_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"types.json#/abc\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    public void Negative_index_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"types.json#/-1\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void Package_path_with_traversal_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"../escape.json#/0\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    public void Package_path_with_backslash_traversal_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"..\\\\escape.json#/0\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    public void Rooted_posix_package_path_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"/tmp/types.json#/0\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    public void Rooted_windows_drive_package_path_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":\"C:/temp/types.json#/0\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    [DataRow(".#/0")]
    [DataRow("./#/0")]
    [DataRow("types.json/#/0")]
    [DataRow("C:types.json#/0")]
    public void Non_file_or_context_dependent_package_path_is_rejected(string reference)
    {
        var (ctx, root) = ParseJson($"{{\"$ref\":\"{reference}\"}}");

        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);

        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    public void Missing_dollar_ref_property_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"ref\":\"#/0\"}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceObjectInvalid);
    }

    [TestMethod]
    public void Non_string_dollar_ref_is_rejected()
    {
        var (ctx, root) = ParseJson("{\"$ref\":42}");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceObjectInvalid);
    }

    [TestMethod]
    public void Non_object_reference_value_is_rejected()
    {
        var (ctx, root) = ParseJson("\"#/0\"");
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        result.IsValid.Should().BeFalse();
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceObjectInvalid);
    }

    [TestMethod]
    public void Reference_object_with_extra_properties_reports_unknown_property_in_canonical_mode()
    {
        var (ctx, root) = ParseJson(
            "{\"$ref\":\"#/0\",\"extra\":1}",
            PackageDocumentKind.TypeFile);
        ctx.Options.Mode.Should().Be(TypePackageValidationMode.CanonicalWriter); // default
        var result = ReferenceSyntax.Validate(root, "prop", "/prop", ctx);
        // $ref itself is valid
        result.IsValid.Should().BeTrue();
        // extra property rejected
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnknownProperty);
    }
}
