// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Azure.Bicep.Types.Validation;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Graph;

[TestClass]
public class SemanticGraphValidatorTests
{
    private static System.Collections.Generic.IReadOnlyList<TypeValidationDiagnostic> Validate(
        string indexJson, InMemoryPackageFileSystem fs)
    {
        var index = GraphTestHelpers.Document("index.json", indexJson);
        var provider = new PackageDocumentProvider(fs, index, new TypePackageValidationOptions());
        return SemanticGraphValidator.Validate(provider, index, new TypePackageValidationOptions());
    }

    private static string ResourceIndex(string refValue) =>
        "{\"resources\":{\"My.Rp/x@2026-01-01\":{\"$ref\":\"" + refValue + "\"}}," +
        "\"resourceFunctions\":{},\"namespaceFunctions\":[]}";

    [TestMethod]
    public void Valid_graph_produces_no_diagnostics()
    {
        const string types = @"[
  { ""$type"": ""StringType"" },
  { ""$type"": ""ObjectType"", ""name"": ""o"",
    ""properties"": { ""p"": { ""type"": { ""$ref"": ""#/0"" }, ""flags"": 0 } } },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""#/1"" }, ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);
        Validate(ResourceIndex("types.json#/2"), fs).Should().BeEmpty();
    }

    [TestMethod]
    public void Missing_referenced_file_reports_bcpvt017()
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json",
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"missing.json#/0\"},\"readableScopes\":8,\"writableScopes\":8}]");

        var diagnostics = Validate(ResourceIndex("types.json#/0"), fs);

        diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferencedTypeFileMissing);
    }

    [TestMethod]
    public void Out_of_range_reference_reports_bcpvt019()
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json",
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/99\"},\"readableScopes\":8,\"writableScopes\":8}]");

        Validate(ResourceIndex("types.json#/0"), fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceIndexOutOfRange);
    }

    [TestMethod]
    public void Wrong_top_level_target_kind_reports_bcpvt020_with_related_location()
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json",
            "[{\"$type\":\"ObjectType\",\"name\":\"notAResource\",\"properties\":{}}]");

        var diagnostic = Validate(ResourceIndex("types.json#/0"), fs).Should().ContainSingle().Subject;

        diagnostic.Code.Should().Be(TypeValidationDiagnosticCodes.TopLevelTargetKindMismatch);
        diagnostic.RelatedLocations.Should().ContainSingle()
            .Which.Message.Should().Be("Target type is declared here.");
    }

    [TestMethod]
    public void Nested_wrong_target_kind_reports_bcpvt021()
    {
        const string types = @"[
  { ""$type"": ""ObjectType"", ""name"": ""o"",
    ""properties"": { ""self"": { ""type"": { ""$ref"": ""#/1"" }, ""flags"": 0 } } },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);

        Validate(ResourceIndex("types.json#/1"), fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.NestedTargetKindMismatch);
    }

    [TestMethod]
    public void Every_edge_to_a_shared_node_is_kind_checked_once_per_edge()
    {
        // Two properties target the same ResourceType node; the node is traversed once but
        // both edges must produce a nested-kind-mismatch diagnostic.
        const string types = @"[
  { ""$type"": ""ObjectType"", ""name"": ""o"", ""properties"": {
      ""a"": { ""type"": { ""$ref"": ""#/1"" }, ""flags"": 0 },
      ""b"": { ""type"": { ""$ref"": ""#/1"" }, ""flags"": 0 } } },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);

        var diagnostics = Validate(ResourceIndex("types.json#/1"), fs);

        diagnostics.Should().HaveCount(2);
        diagnostics.Should().OnlyContain(d => d.Code == TypeValidationDiagnosticCodes.NestedTargetKindMismatch);
    }

    [TestMethod]
    public void Cyclic_graph_terminates_without_overflow()
    {
        // Self-referential object property (ObjectType is a valid value type), plus a deep chain.
        const string types = @"[
  { ""$type"": ""ObjectType"", ""name"": ""selfref"",
    ""properties"": { ""self"": { ""type"": { ""$ref"": ""#/0"" }, ""flags"": 0 } } },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);

        Validate(ResourceIndex("types.json#/1"), fs).Should().BeEmpty();
    }

    // ── Recovery: wrong-kind targets are not traversed ───────────────────────

    [TestMethod]
    public void Wrong_top_level_kind_does_not_traverse_into_target_edges()
    {
        // The resource root points at an ObjectType (wrong kind) whose property targets a
        // ResourceType. Recovery must stop at the top-level mismatch and NOT descend into the
        // ObjectType, so only the single BCPVT020 is reported.
        const string types = @"[
  { ""$type"": ""ObjectType"", ""name"": ""wrongRoot"",
    ""properties"": { ""p"": { ""type"": { ""$ref"": ""#/1"" }, ""flags"": 0 } } },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);

        Validate(ResourceIndex("types.json#/0"), fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TopLevelTargetKindMismatch);
    }

    // ── Wrong-kind roots per root type ───────────────────────────────────────

    [TestMethod]
    public void Wrong_resource_function_root_kind_reports_bcpvt020()
    {
        const string index =
            "{\"resources\":{},\"resourceFunctions\":{\"My.Rp/x\":{\"2026-01-01\":[{\"$ref\":\"types.json#/0\"}]}}," +
            "\"namespaceFunctions\":[]}";
        var fs = new InMemoryPackageFileSystem().AddText("types.json",
            "[{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}]");

        Validate(index, fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TopLevelTargetKindMismatch);
    }

    [TestMethod]
    public void Wrong_namespace_function_root_kind_reports_bcpvt020()
    {
        const string index =
            "{\"resources\":{},\"resourceFunctions\":{},\"namespaceFunctions\":[{\"$ref\":\"types.json#/0\"}]}";
        var fs = new InMemoryPackageFileSystem().AddText("types.json",
            "[{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}]");

        Validate(index, fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TopLevelTargetKindMismatch);
    }

    [TestMethod]
    public void Wrong_configuration_type_kind_reports_bcpvt020()
    {
        const string index =
            "{\"resources\":{},\"resourceFunctions\":{},\"namespaceFunctions\":[]," +
            "\"settings\":{\"name\":\"s\",\"version\":\"1\",\"isSingleton\":true,\"configurationType\":{\"$ref\":\"types.json#/0\"}}}";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", "[{\"$type\":\"StringType\"}]");

        Validate(index, fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TopLevelTargetKindMismatch);
    }

    [TestMethod]
    public void Wrong_fallback_resource_type_kind_reports_bcpvt020()
    {
        const string index =
            "{\"resources\":{},\"resourceFunctions\":{},\"namespaceFunctions\":[]," +
            "\"fallbackResourceType\":{\"$ref\":\"types.json#/0\"}}";
        var fs = new InMemoryPackageFileSystem().AddText("types.json",
            "[{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}]");

        Validate(index, fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.TopLevelTargetKindMismatch);
    }

    // ── Nested wrong-kind targets per role ───────────────────────────────────

    [TestMethod]
    public void Resource_type_function_wrong_target_kind_reports_bcpvt021()
    {
        const string types = @"[
  { ""$type"": ""ObjectType"", ""name"": ""notAFunction"", ""properties"": {} },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/x@2026-01-01"", ""body"": { ""$ref"": ""#/0"" },
    ""functions"": { ""list"": { ""type"": { ""$ref"": ""#/0"" } } },
    ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);

        Validate(ResourceIndex("types.json#/1"), fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.NestedTargetKindMismatch);
    }

    [TestMethod]
    public void Discriminated_object_base_property_wrong_target_kind_reports_bcpvt021()
    {
        const string types = @"[
  { ""$type"": ""DiscriminatedObjectType"", ""name"": ""d"", ""discriminator"": ""kind"",
    ""baseProperties"": { ""bad"": { ""type"": { ""$ref"": ""#/1"" }, ""flags"": 0 } },
    ""elements"": {} },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/x@2026-01-01"", ""body"": { ""$ref"": ""#/0"" },
    ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);

        Validate(ResourceIndex("types.json#/1"), fs).Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.NestedTargetKindMismatch);
    }

    // ── Duplicate missing-file references ────────────────────────────────────

    [TestMethod]
    public void Duplicate_missing_file_references_report_one_diagnostic_per_source_ref()
    {
        const string index =
            "{\"resources\":{" +
            "\"My.Rp/a@2026-01-01\":{\"$ref\":\"gone.json#/0\"}," +
            "\"My.Rp/b@2026-01-01\":{\"$ref\":\"gone.json#/0\"}}," +
            "\"resourceFunctions\":{},\"namespaceFunctions\":[]}";
        var fs = new InMemoryPackageFileSystem();

        var diagnostics = Validate(index, fs);

        diagnostics.Should().HaveCount(2);
        diagnostics.Should().OnlyContain(d => d.Code == TypeValidationDiagnosticCodes.ReferencedTypeFileMissing);
    }

    [TestMethod]
    public void Graph_time_read_failure_reports_source_ref_location()
    {
        var fs = new InMemoryPackageFileSystem()
            .AddText("types.json",
                "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
                "\"body\":{\"$ref\":\"bad.json#/0\"},\"readableScopes\":8,\"writableScopes\":8}]")
            .AddUnreadable("bad.json");

        var diagnostic = Validate(ResourceIndex("types.json#/0"), fs).Should().ContainSingle().Subject;

        diagnostic.Code.Should().Be(TypeValidationDiagnosticCodes.PackageFileReadFailed);
        diagnostic.Path.Should().Be("types.json");
        diagnostic.JsonPointer.Should().Be("/0/body/$ref");
        diagnostic.Line.Should().NotBeNull();
    }
}
