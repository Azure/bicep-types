// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Azure.Bicep.Types.Validation.Graph;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Graph;

[TestClass]
public class TypeGraphBuilderTests
{
    // ── Roots ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void ExtractRoots_reads_every_index_root_kind()
    {
        const string indexJson = @"{
  ""resources"": { ""My.Rp/things@2026-01-01"": { ""$ref"": ""types.json#/0"" } },
  ""resourceFunctions"": { ""My.Rp/things"": { ""2026-01-01"": [ { ""$ref"": ""types.json#/1"" } ] } },
  ""namespaceFunctions"": [ { ""$ref"": ""types.json#/2"" } ],
  ""settings"": { ""configurationType"": { ""$ref"": ""types.json#/3"" } },
  ""fallbackResourceType"": { ""$ref"": ""types.json#/4"" }
}";
        var index = GraphTestHelpers.Document("index.json", indexJson);

        var roots = TypeGraphBuilder.ExtractRoots(index);

        roots.Select(r => r.Role).Should().BeEquivalentTo(new[]
        {
            TypeReferenceRole.ResourceRoot,
            TypeReferenceRole.ResourceFunctionRoot,
            TypeReferenceRole.NamespaceFunctionRoot,
            TypeReferenceRole.ConfigurationType,
            TypeReferenceRole.FallbackResourceType,
        });
        roots.Single(r => r.Role == TypeReferenceRole.ResourceRoot).Description
            .Should().Be("Resource entry 'My.Rp/things@2026-01-01'");
        roots.Single(r => r.Role == TypeReferenceRole.ResourceFunctionRoot).Description
            .Should().Be("Resource function 'My.Rp/things@2026-01-01[0]'");
    }

    [TestMethod]
    public void ExtractRoots_returns_empty_for_non_object_root()
    {
        var index = GraphTestHelpers.Document("index.json", "[]");
        TypeGraphBuilder.ExtractRoots(index).Should().BeEmpty();
    }

    [TestMethod]
    public void ExtractRoots_skips_malformed_reference_objects()
    {
        const string indexJson = @"{
  ""resources"": {
    ""a"": { ""$ref"": 5 },
    ""b"": { ""nope"": ""x"" },
    ""c"": { ""$ref"": ""../escape.json#/0"" }
  },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";
        var index = GraphTestHelpers.Document("index.json", indexJson);
        TypeGraphBuilder.ExtractRoots(index).Should().BeEmpty();
    }

    // ── Nodes ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void BuildNodes_returns_null_for_non_array_root()
    {
        var doc = GraphTestHelpers.Document("types.json", "{\"$type\":\"StringType\"}");
        TypeGraphBuilder.BuildNodes(doc).Should().BeNull();
    }

    [TestMethod]
    public void BuildNodes_maps_index_and_marks_unusable_elements_null()
    {
        const string json = @"[
  { ""$type"": ""StringType"" },
  42,
  { ""name"": ""no-discriminator"" },
  { ""$type"": ""NotARealType"" },
  { ""$type"": ""ObjectType"", ""name"": ""o"", ""properties"": {} }
]";
        var doc = GraphTestHelpers.Document("types.json", json);

        var nodes = TypeGraphBuilder.BuildNodes(doc);

        nodes.Should().NotBeNull();
        nodes!.Count.Should().Be(5);
        nodes[0].Should().NotBeNull();
        nodes[0]!.Discriminator.Should().Be("StringType");
        nodes[1].Should().BeNull(); // primitive
        nodes[2].Should().BeNull(); // no $type
        nodes[3].Should().BeNull(); // unknown discriminator
        nodes[4]!.Discriminator.Should().Be("ObjectType");
        nodes[4]!.Id.Index.Should().Be(4);
        nodes[4]!.JsonPointer.Should().Be("/4");
    }

    // ── Edges ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void ExtractEdges_reads_resource_body_and_functions()
    {
        const string json = @"[
  { ""$type"": ""ResourceType"", ""name"": ""r"", ""body"": { ""$ref"": ""#/1"" },
    ""functions"": { ""list"": { ""type"": { ""$ref"": ""#/2"" } } },
    ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var doc = GraphTestHelpers.Document("types.json", json);
        var node = TypeGraphBuilder.BuildNodes(doc)![0]!;

        var edges = TypeGraphBuilder.ExtractEdges(node);

        edges.Select(e => e.Role).Should().BeEquivalentTo(new[]
        {
            TypeReferenceRole.ResourceBody,
            TypeReferenceRole.ResourceTypeFunction,
        });
        edges.Single(e => e.Role == TypeReferenceRole.ResourceBody).Reference.Index.Should().Be(1);
    }

    [TestMethod]
    public void ExtractEdges_reads_object_properties_and_additional_properties()
    {
        const string json = @"[
  { ""$type"": ""ObjectType"", ""name"": ""o"",
    ""properties"": { ""p"": { ""type"": { ""$ref"": ""#/1"" }, ""flags"": 0 } },
    ""additionalProperties"": { ""$ref"": ""#/2"" } }
]";
        var doc = GraphTestHelpers.Document("types.json", json);
        var node = TypeGraphBuilder.BuildNodes(doc)![0]!;

        var edges = TypeGraphBuilder.ExtractEdges(node);

        edges.Select(e => e.Role).Should().BeEquivalentTo(new[]
        {
            TypeReferenceRole.ObjectPropertyType,
            TypeReferenceRole.AdditionalProperties,
        });
        edges.Single(e => e.Role == TypeReferenceRole.ObjectPropertyType).MemberName.Should().Be("p");
    }

    [TestMethod]
    public void ExtractEdges_reads_array_and_union_members()
    {
        const string json = @"[
  { ""$type"": ""ArrayType"", ""itemType"": { ""$ref"": ""#/1"" } },
  { ""$type"": ""UnionType"", ""elements"": [ { ""$ref"": ""#/0"" }, { ""$ref"": ""#/1"" } ] }
]";
        var doc = GraphTestHelpers.Document("types.json", json);
        var nodes = TypeGraphBuilder.BuildNodes(doc)!;

        TypeGraphBuilder.ExtractEdges(nodes[0]!).Should().ContainSingle()
            .Which.Role.Should().Be(TypeReferenceRole.ArrayItem);
        TypeGraphBuilder.ExtractEdges(nodes[1]!).Select(e => e.Role)
            .Should().OnlyContain(r => r == TypeReferenceRole.UnionMember);
    }

    [TestMethod]
    public void ExtractEdges_skips_malformed_references()
    {
        const string json = @"[
  { ""$type"": ""ArrayType"", ""itemType"": { ""$ref"": ""/rooted.json#/0"" } }
]";
        var doc = GraphTestHelpers.Document("types.json", json);
        var node = TypeGraphBuilder.BuildNodes(doc)![0]!;
        TypeGraphBuilder.ExtractEdges(node).Should().BeEmpty();
    }

    [TestMethod]
    public void ExtractEdges_value_kinds_have_no_edges()
    {
        var doc = GraphTestHelpers.Document("types.json", "[{\"$type\":\"StringType\"}]");
        var node = TypeGraphBuilder.BuildNodes(doc)![0]!;
        TypeGraphBuilder.ExtractEdges(node).Should().BeEmpty();
    }

    [TestMethod]
    public void ExtractEdges_reads_discriminated_object_base_properties_and_elements()
    {
        const string json = @"[
  { ""$type"": ""ObjectType"", ""name"": ""a"", ""properties"": {} },
  { ""$type"": ""DiscriminatedObjectType"", ""name"": ""d"", ""discriminator"": ""kind"",
    ""baseProperties"": { ""id"": { ""type"": { ""$ref"": ""#/0"" }, ""flags"": 0 } },
    ""elements"": { ""a"": { ""$ref"": ""#/0"" } } }
]";
        var doc = GraphTestHelpers.Document("types.json", json);
        var node = TypeGraphBuilder.BuildNodes(doc)![1]!;

        var edges = TypeGraphBuilder.ExtractEdges(node);

        edges.Select(e => e.Role).Should().BeEquivalentTo(new[]
        {
            TypeReferenceRole.ObjectPropertyType,
            TypeReferenceRole.DiscriminatedObjectElement,
        });
        edges.Single(e => e.Role == TypeReferenceRole.ObjectPropertyType).MemberName.Should().Be("id");
        edges.Single(e => e.Role == TypeReferenceRole.DiscriminatedObjectElement).MemberName.Should().Be("a");
    }

    [TestMethod]
    public void ExtractEdges_reads_function_type_parameters_and_output()
    {
        const string json = @"[
  { ""$type"": ""FunctionType"",
    ""parameters"": [
      { ""name"": ""a"", ""type"": { ""$ref"": ""#/1"" } },
      { ""name"": ""b"", ""type"": { ""$ref"": ""#/2"" } }
    ],
    ""output"": { ""$ref"": ""#/3"" } }
]";
        var doc = GraphTestHelpers.Document("types.json", json);
        var node = TypeGraphBuilder.BuildNodes(doc)![0]!;

        var edges = TypeGraphBuilder.ExtractEdges(node);

        edges.Select(e => e.Role).Should().BeEquivalentTo(new[]
        {
            TypeReferenceRole.FunctionParameter,
            TypeReferenceRole.FunctionParameter,
            TypeReferenceRole.FunctionOutput,
        });
        edges.Where(e => e.Role == TypeReferenceRole.FunctionParameter)
            .Select(e => e.Reference.Index).Should().BeEquivalentTo(new[] { 1, 2 });
        edges.Where(e => e.Role == TypeReferenceRole.FunctionParameter)
            .Select(e => e.MemberName).Should().BeEquivalentTo(new[] { "[0]", "[1]" });
        edges.Single(e => e.Role == TypeReferenceRole.FunctionOutput).Reference.Index.Should().Be(3);
    }

    [TestMethod]
    public void ExtractEdges_function_type_skips_parameters_without_type_reference()
    {
        // A parameter that is not an object, or lacks a well-formed 'type' reference, yields no
        // edge; a missing 'output' yields no output edge. The structural layer owns those shapes.
        const string json = @"[
  { ""$type"": ""FunctionType"",
    ""parameters"": [
      42,
      { ""name"": ""noType"" },
      { ""name"": ""bad"", ""type"": { ""$ref"": ""/rooted.json#/0"" } },
      { ""name"": ""ok"", ""type"": { ""$ref"": ""#/1"" } }
    ] }
]";
        var doc = GraphTestHelpers.Document("types.json", json);
        var node = TypeGraphBuilder.BuildNodes(doc)![0]!;

        var edges = TypeGraphBuilder.ExtractEdges(node);

        edges.Should().ContainSingle()
            .Which.Should().Match<TypeGraphEdge>(e =>
                e.Role == TypeReferenceRole.FunctionParameter && e.Reference.Index == 1 && e.MemberName == "[3]");
    }

    // ── Canonical reference-object shape ─────────────────────────────────────

    [TestMethod]
    public void ExtractRoots_skips_reference_object_with_extra_property()
    {
        // The structural layer reports the extra property (BCPVT014); the graph layer must not
        // follow such a non-canonical reference (avoids a follow-on graph diagnostic).
        const string indexJson = @"{
  ""resources"": { ""a"": { ""$ref"": ""types.json#/0"", ""extra"": true } },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";
        var index = GraphTestHelpers.Document("index.json", indexJson);
        TypeGraphBuilder.ExtractRoots(index).Should().BeEmpty();
    }

    [TestMethod]
    public void ExtractEdges_skips_reference_object_with_extra_property()
    {
        const string json = @"[
  { ""$type"": ""ArrayType"", ""itemType"": { ""$ref"": ""#/0"", ""extra"": 1 } }
]";
        var doc = GraphTestHelpers.Document("types.json", json);
        var node = TypeGraphBuilder.BuildNodes(doc)![0]!;
        TypeGraphBuilder.ExtractEdges(node).Should().BeEmpty();
    }
}
