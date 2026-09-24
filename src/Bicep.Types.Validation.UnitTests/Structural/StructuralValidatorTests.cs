// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.Structural;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Structural;

[TestClass]
public class StructuralValidatorTests
{
    // ── Phase gates ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Missing_index_document_produces_no_structural_diagnostics()
    {
        // Null index document = fatal read failure already reported by PackageReader
        var docSet = new JsonDocumentSet(null, new PackageDocument[0]);
        var diagnostics = StructuralValidator.Validate(docSet, new TypePackageValidationOptions());
        diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Malformed_index_root_prevents_type_file_validation()
    {
        // index.json root is an array (wrong shape) — no child inspection
        // A type file is also included but should NOT produce diagnostics
        byte[] indexBytes = System.Text.Encoding.UTF8.GetBytes("[1,2]");
        SourceMap.TryParse(indexBytes, "index.json", out var indexRoot, out var indexSm, out _);
        var indexDoc = new PackageDocument("index.json", PackageDocumentKind.Index, indexRoot!, indexSm);

        byte[] typeBytes = System.Text.Encoding.UTF8.GetBytes("[{\"$type\":\"StringType\"}]");
        SourceMap.TryParse(typeBytes, "types.json", out var typeRoot, out var typeSm, out _);
        var typeDoc = new PackageDocument("types.json", PackageDocumentKind.TypeFile, typeRoot!, typeSm);

        var docSet = new JsonDocumentSet(indexDoc, new[] { typeDoc });
        var diagnostics = StructuralValidator.Validate(docSet, new TypePackageValidationOptions());

        // Only the IndexRootMustBeObject diagnostic should appear; no type-file errors
        diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.IndexRootMustBeObject);
    }

    [TestMethod]
    public void Wrong_type_file_root_shape_prevents_element_inspection()
    {
        // type file root is an object, not an array — should get TypeFileRootMustBeArray
        // but no TypeObjectDiscriminator errors
        var indexDoc = CreateDoc("index.json", PackageDocumentKind.Index,
            "{\"resources\":{},\"resourceFunctions\":{},\"namespaceFunctions\":[]}");
        var typeDoc = CreateDoc("types.json", PackageDocumentKind.TypeFile,
            "{\"$type\":\"StringType\"}"); // object, not array

        var docSet = new JsonDocumentSet(indexDoc, new[] { typeDoc });
        var diagnostics = StructuralValidator.Validate(docSet, new TypePackageValidationOptions());

        diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeFileRootMustBeArray);
    }

    [TestMethod]
    public void Non_object_type_file_element_prevents_type_object_inspection_for_that_element()
    {
        var indexDoc = CreateDoc("index.json", PackageDocumentKind.Index,
            "{\"resources\":{},\"resourceFunctions\":{},\"namespaceFunctions\":[]}");
        var typeDoc = CreateDoc("types.json", PackageDocumentKind.TypeFile,
            "[42]"); // element is not an object

        var docSet = new JsonDocumentSet(indexDoc, new[] { typeDoc });
        var diagnostics = StructuralValidator.Validate(docSet, new TypePackageValidationOptions());

        // Should get TypeFileElementMustBeObject but NOT TypeObjectDiscriminatorMissing
        diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeFileElementMustBeObject);
    }

    [TestMethod]
    public void Type_object_with_missing_dollar_type_prevents_kind_specific_checks()
    {
        // An object without $type must not trigger RequiredPropertyMissing for kind-specific fields
        var indexDoc = CreateDoc("index.json", PackageDocumentKind.Index,
            "{\"resources\":{},\"resourceFunctions\":{},\"namespaceFunctions\":[]}");
        var typeDoc = CreateDoc("types.json", PackageDocumentKind.TypeFile,
            "[{\"name\":\"foo\"}]"); // no $type, so kind-specific 'name' check should not run

        var docSet = new JsonDocumentSet(indexDoc, new[] { typeDoc });
        var diagnostics = StructuralValidator.Validate(docSet, new TypePackageValidationOptions());

        diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TypeObjectDiscriminatorMissing);
    }

    [TestMethod]
    public void Malformed_ref_in_index_does_not_produce_graph_style_diagnostics()
    {
        // A bad ref syntax should report ReferenceSyntaxInvalid, NOT an unresolved-reference
        // graph diagnostic (which is out of phase-2 scope)
        var indexDoc = CreateDoc("index.json", PackageDocumentKind.Index,
            "{\"resources\":{\"S/r@v1\":\"not-a-ref-object\"},\"resourceFunctions\":{},\"namespaceFunctions\":[]}");

        var docSet = new JsonDocumentSet(indexDoc, new PackageDocument[0]);
        var diagnostics = StructuralValidator.Validate(docSet, new TypePackageValidationOptions());

        // ReferenceObjectInvalid (structural) but nothing graph-style like "unresolved reference"
        diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceObjectInvalid);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static PackageDocument CreateDoc(string relPath, PackageDocumentKind kind, string json)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        SourceMap.TryParse(bytes, relPath, out var root, out var sm, out _);
        return new PackageDocument(relPath, kind, root!, sm);
    }
}
