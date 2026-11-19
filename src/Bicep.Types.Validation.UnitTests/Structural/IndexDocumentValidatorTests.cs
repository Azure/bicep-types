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
public class IndexDocumentValidatorTests
{
    private static (StructuralValidationContext ctx, JsonShapeReader reader) CreateContext(
        string indexJson,
        TypePackageValidationMode mode = TypePackageValidationMode.CanonicalWriter)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(indexJson);
        SourceMap.TryParse(bytes, "index.json", out var root, out var sm, out _);
        var doc = new PackageDocument("index.json", PackageDocumentKind.Index, root!, sm);
        var options = new TypePackageValidationOptions { Mode = mode };
        var ctx = new StructuralValidationContext(options);
        ctx.SetCurrentDocument(doc);
        var reader = new JsonShapeReader(ctx);
        return (ctx, reader);
    }

    // ── Root shape ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Non_object_root_reports_index_root_must_be_object()
    {
        var (ctx, reader) = CreateContext("[1,2,3]");
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.IndexRootMustBeObject);
    }

    // ── Required top-level fields ────────────────────────────────────────────

    [TestMethod]
    public void Missing_resources_reports_required_property_missing()
    {
        var (ctx, reader) = CreateContext("{\"resourceFunctions\":{},\"namespaceFunctions\":[]}");
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.RequiredPropertyMissing && d.Message.Contains("resources"));
    }

    [TestMethod]
    public void Missing_resource_functions_reports_required_property_missing()
    {
        var (ctx, reader) = CreateContext("{\"resources\":{},\"namespaceFunctions\":[]}");
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.RequiredPropertyMissing && d.Message.Contains("resourceFunctions"));
    }

    [TestMethod]
    public void Missing_namespace_functions_reports_required_property_missing()
    {
        var (ctx, reader) = CreateContext("{\"resources\":{},\"resourceFunctions\":{}}");
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.RequiredPropertyMissing && d.Message.Contains("namespaceFunctions"));
    }

    // ── resources map ────────────────────────────────────────────────────────

    [TestMethod]
    public void Non_object_resources_reports_property_type_mismatch()
    {
        var (ctx, reader) = CreateContext("{\"resources\":\"bad\",\"resourceFunctions\":{},\"namespaceFunctions\":[]}");
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.PropertyTypeMismatch);
    }

    [TestMethod]
    public void Resource_entry_with_invalid_ref_reports_reference_object_invalid()
    {
        var json = @"{
  ""resources"": { ""S/r@2026"": ""not-a-ref"" },
  ""resourceFunctions"": {}, ""namespaceFunctions"": []
}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceObjectInvalid);
    }

    // ── resourceFunctions ────────────────────────────────────────────────────

    [TestMethod]
    public void Resource_functions_must_be_nested_object_map()
    {
        var json = @"{
  ""resources"": {},
  ""resourceFunctions"": {
    ""S/r@2026"": {
      ""2026-01-01"": [{""$ref"":""types.json#/0""}]
    }
  },
  ""namespaceFunctions"": []
}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    // ── namespaceFunctions ───────────────────────────────────────────────────

    [TestMethod]
    public void Namespace_functions_must_be_array_of_refs()
    {
        var json = @"{
  ""resources"": {}, ""resourceFunctions"": {},
  ""namespaceFunctions"": [{""$ref"":""types.json#/1""}]
}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    // ── settings ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Settings_object_with_required_fields_is_valid()
    {
        var json = @"{
  ""resources"":{},""resourceFunctions"":{},""namespaceFunctions"":[],
  ""settings"":{""name"":""Prov"",""version"":""1.0"",""isSingleton"":true}
}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    [TestMethod]
    public void Settings_missing_name_reports_required_property_missing()
    {
        var json = @"{
  ""resources"":{},""resourceFunctions"":{},""namespaceFunctions"":[],
  ""settings"":{""version"":""1.0"",""isSingleton"":true}
}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle(d =>
            d.Code == TypeValidationDiagnosticCodes.RequiredPropertyMissing && d.Message.Contains("name"));
    }

    [TestMethod]
    public void Settings_missing_isSingleton_reports_required_property_missing()
    {
        var json = @"{
  ""resources"":{},""resourceFunctions"":{},""namespaceFunctions"":[],
  ""settings"":{""name"":""P"",""version"":""1.0""}
}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle(d => d.Message.Contains("isSingleton"));
    }

    [TestMethod]
    public void Settings_is_preview_and_is_deprecated_accept_null()
    {
        var json = @"{
  ""resources"":{},""resourceFunctions"":{},""namespaceFunctions"":[],
  ""settings"":{""name"":""P"",""version"":""1.0"",""isSingleton"":false,
    ""isPreview"":null,""isDeprecated"":null}
}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    // ── fallbackResourceType ─────────────────────────────────────────────────

    [TestMethod]
    public void Valid_fallback_resource_type_ref_is_accepted()
    {
        var json = @"{
  ""resources"":{},""resourceFunctions"":{},""namespaceFunctions"":[],
  ""fallbackResourceType"":{""$ref"":""types.json#/0""}
}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().BeEmpty();
    }

    // ── Unknown top-level fields ─────────────────────────────────────────────

    [TestMethod]
    public void Unknown_top_level_field_reports_unknown_property_in_canonical_mode()
    {
        var json = @"{""resources"":{},""resourceFunctions"":{},""namespaceFunctions"":[],""extra"":1}";
        var (ctx, reader) = CreateContext(json, TypePackageValidationMode.CanonicalWriter);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnknownProperty);
    }

    [TestMethod]
    public void Unknown_top_level_field_reports_unknown_property_in_compatible_reader_mode()
    {
        // index.json has no documented legacy top-level fields, so arbitrary unknown
        // fields are rejected in CompatibleReader as well as CanonicalWriter.
        var json = @"{""resources"":{},""resourceFunctions"":{},""namespaceFunctions"":[],""extra"":1}";
        var (ctx, reader) = CreateContext(json, TypePackageValidationMode.CompatibleReader);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnknownProperty);
    }

    // ── JSON pointer escaping (RFC 6901) ─────────────────────────────────────

    [TestMethod]
    public void Resource_key_with_special_characters_is_rfc6901_escaped_in_pointer()
    {
        // A resource type key containing '/' and '~' must be escaped as '~1' and '~0'
        // (in that order) in the emitted JSON pointer.
        var json = @"{""resources"":{""a/b~c"":""not-a-ref""},""resourceFunctions"":{},""namespaceFunctions"":[]}";
        var (ctx, reader) = CreateContext(json);
        IndexDocumentValidator.Validate(reader, ctx);
        ctx.GetDiagnostics().Should().ContainSingle()
            .Which.JsonPointer.Should().Be("/resources/a~1b~0c");
    }
}
