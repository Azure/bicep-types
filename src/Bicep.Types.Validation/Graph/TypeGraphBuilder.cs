// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.Structural;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// Extracts graph roots (from <c>index.json</c>), graph nodes (type objects), and graph
    /// edges (nested references) from already-parsed package documents.  Only well-formed,
    /// structurally usable references are surfaced; malformed references are silently skipped
    /// because they are already reported by the structural layer.
    /// </summary>
    internal static class TypeGraphBuilder
    {
        // ── Roots ────────────────────────────────────────────────────────────────

        /// <summary>Extracts the graph roots referenced by <c>index.json</c>.</summary>
        public static IReadOnlyList<TypeGraphRoot> ExtractRoots(PackageDocument indexDocument)
        {
            if (indexDocument == null) { throw new ArgumentNullException(nameof(indexDocument)); }

            var roots = new List<TypeGraphRoot>();
            var root = indexDocument.Root;
            if (root.Kind != JsonValueKind.Object)
            {
                return roots;
            }

            // resources: name -> ref
            if (root.TryGetProperty("resources", out var resources) && resources.Kind == JsonValueKind.Object)
            {
                foreach (var entry in resources.Properties)
                {
                    string pointer = "/resources/" + JsonPointerEscape(entry.Name);
                    if (TryParseReferenceObject(entry.Value, indexDocument, pointer, out var reference))
                    {
                        roots.Add(new TypeGraphRoot(
                            TypeGraphRootKind.Resource,
                            $"Resource entry '{entry.Name}'",
                            TypeReferenceRole.ResourceRoot,
                            reference));
                    }
                }
            }

            // resourceFunctions: resourceType -> apiVersion -> [ref, ...]
            if (root.TryGetProperty("resourceFunctions", out var resourceFunctions) && resourceFunctions.Kind == JsonValueKind.Object)
            {
                foreach (var rtEntry in resourceFunctions.Properties)
                {
                    if (rtEntry.Value.Kind != JsonValueKind.Object) { continue; }
                    string rtPointer = "/resourceFunctions/" + JsonPointerEscape(rtEntry.Name);

                    foreach (var avEntry in rtEntry.Value.Properties)
                    {
                        if (avEntry.Value.Kind != JsonValueKind.Array) { continue; }
                        string avPointer = rtPointer + "/" + JsonPointerEscape(avEntry.Name);

                        for (int i = 0; i < avEntry.Value.Elements.Count; i++)
                        {
                            string pointer = avPointer + "/" + i;
                            if (TryParseReferenceObject(avEntry.Value.Elements[i], indexDocument, pointer, out var reference))
                            {
                                roots.Add(new TypeGraphRoot(
                                    TypeGraphRootKind.ResourceFunction,
                                    $"Resource function '{rtEntry.Name}@{avEntry.Name}[{i}]'",
                                    TypeReferenceRole.ResourceFunctionRoot,
                                    reference));
                            }
                        }
                    }
                }
            }

            // namespaceFunctions: [ref, ...]
            if (root.TryGetProperty("namespaceFunctions", out var namespaceFunctions) && namespaceFunctions.Kind == JsonValueKind.Array)
            {
                for (int i = 0; i < namespaceFunctions.Elements.Count; i++)
                {
                    string pointer = "/namespaceFunctions/" + i;
                    if (TryParseReferenceObject(namespaceFunctions.Elements[i], indexDocument, pointer, out var reference))
                    {
                        roots.Add(new TypeGraphRoot(
                            TypeGraphRootKind.NamespaceFunction,
                            $"Namespace function [{i}]",
                            TypeReferenceRole.NamespaceFunctionRoot,
                            reference));
                    }
                }
            }

            // settings.configurationType: ref
            if (root.TryGetProperty("settings", out var settings) && settings.Kind == JsonValueKind.Object &&
                settings.TryGetProperty("configurationType", out var configurationType))
            {
                if (TryParseReferenceObject(configurationType, indexDocument, "/settings/configurationType", out var reference))
                {
                    roots.Add(new TypeGraphRoot(
                        TypeGraphRootKind.ConfigurationType,
                        "Settings configurationType",
                        TypeReferenceRole.ConfigurationType,
                        reference));
                }
            }

            // fallbackResourceType: ref
            if (root.TryGetProperty("fallbackResourceType", out var fallback))
            {
                if (TryParseReferenceObject(fallback, indexDocument, "/fallbackResourceType", out var reference))
                {
                    roots.Add(new TypeGraphRoot(
                        TypeGraphRootKind.FallbackResourceType,
                        "Fallback resource type",
                        TypeReferenceRole.FallbackResourceType,
                        reference));
                }
            }

            return roots;
        }

        // ── Nodes ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds an index-aligned list of graph nodes for a type file.  Elements that are not
        /// structurally usable type objects (non-object, missing/non-string <c>$type</c>, or an
        /// unsupported discriminator) are represented as <c>null</c>.  Returns <c>null</c> if the
        /// file root is not an array.
        /// </summary>
        public static IReadOnlyList<TypeGraphNode?>? BuildNodes(PackageDocument typeFile)
        {
            if (typeFile == null) { throw new ArgumentNullException(nameof(typeFile)); }

            var root = typeFile.Root;
            if (root.Kind != JsonValueKind.Array)
            {
                return null;
            }

            var nodes = new List<TypeGraphNode?>(root.Elements.Count);
            for (int i = 0; i < root.Elements.Count; i++)
            {
                nodes.Add(BuildNode(typeFile, root.Elements[i], i));
            }
            return nodes;
        }

        private static TypeGraphNode? BuildNode(PackageDocument typeFile, JsonValueNode element, int index)
        {
            if (element.Kind != JsonValueKind.Object)
            {
                return null;
            }

            if (!element.TryGetProperty("$type", out var typeNode) || typeNode.Kind != JsonValueKind.String)
            {
                return null;
            }

            string discriminator = typeNode.StringValue ?? string.Empty;
            if (TypeShapeCatalog.GetDescriptor(discriminator) == null)
            {
                return null;
            }

            var id = new TypeNodeId(typeFile.PackageRelativePath, index);
            var location = typeFile.SourceMap.GetLocation(element.ByteOffset);
            return new TypeGraphNode(id, discriminator, element, typeFile, "/" + index, location);
        }

        // ── Edges ────────────────────────────────────────────────────────────────

        /// <summary>Extracts the outgoing reference edges from a graph node.</summary>
        public static IReadOnlyList<TypeGraphEdge> ExtractEdges(TypeGraphNode node)
        {
            if (node == null) { throw new ArgumentNullException(nameof(node)); }

            var edges = new List<TypeGraphEdge>();
            var doc = node.Document;
            var obj = node.ObjectNode;
            string basePointer = node.JsonPointer;

            switch (node.Discriminator)
            {
                case "ResourceType":
                    AddEdge(edges, node, doc, obj, "body", basePointer, TypeReferenceRole.ResourceBody);
                    AddMapMemberEdges(edges, node, doc, obj, "functions", basePointer, TypeReferenceRole.ResourceTypeFunction, memberValueRefProperty: "type");
                    break;

                case "ObjectType":
                    AddMapMemberEdges(edges, node, doc, obj, "properties", basePointer, TypeReferenceRole.ObjectPropertyType, memberValueRefProperty: "type");
                    AddEdge(edges, node, doc, obj, "additionalProperties", basePointer, TypeReferenceRole.AdditionalProperties);
                    break;

                case "DiscriminatedObjectType":
                    AddMapMemberEdges(edges, node, doc, obj, "baseProperties", basePointer, TypeReferenceRole.ObjectPropertyType, memberValueRefProperty: "type");
                    AddMapMemberEdges(edges, node, doc, obj, "elements", basePointer, TypeReferenceRole.DiscriminatedObjectElement, memberValueRefProperty: null);
                    break;

                case "ArrayType":
                    AddEdge(edges, node, doc, obj, "itemType", basePointer, TypeReferenceRole.ArrayItem);
                    break;

                case "UnionType":
                    AddArrayMemberEdges(edges, node, doc, obj, "elements", basePointer, TypeReferenceRole.UnionMember);
                    break;

                case "FunctionType":
                    AddParameterEdges(edges, node, doc, obj, "parameters", basePointer, TypeReferenceRole.FunctionParameter);
                    AddEdge(edges, node, doc, obj, "output", basePointer, TypeReferenceRole.FunctionOutput);
                    break;

                case "ResourceFunctionType":
                    AddEdge(edges, node, doc, obj, "input", basePointer, TypeReferenceRole.ResourceFunctionInput);
                    AddEdge(edges, node, doc, obj, "output", basePointer, TypeReferenceRole.ResourceFunctionOutput);
                    break;

                case "NamespaceFunctionType":
                    AddParameterEdges(edges, node, doc, obj, "parameters", basePointer, TypeReferenceRole.NamespaceFunctionParameter);
                    AddEdge(edges, node, doc, obj, "outputType", basePointer, TypeReferenceRole.NamespaceFunctionOutput);
                    break;

                default:
                    // Value-only kinds (AnyType, NullType, BooleanType, IntegerType, StringType,
                    // StringLiteralType, BuiltInType) have no outgoing references.
                    break;
            }

            return edges;
        }

        /// <summary>Adds a single edge for a direct reference-valued field, if present and well-formed.</summary>
        private static void AddEdge(
            List<TypeGraphEdge> edges, TypeGraphNode node, PackageDocument doc, JsonValueNode obj,
            string fieldName, string basePointer, TypeReferenceRole role)
        {
            if (!obj.TryGetProperty(fieldName, out var value)) { return; }
            string pointer = basePointer + "/" + fieldName;
            if (TryParseReferenceObject(value, doc, pointer, out var reference))
            {
                edges.Add(new TypeGraphEdge(node.Id, role, reference, fieldName));
            }
        }

        /// <summary>
        /// Adds edges for an object-map field whose members either are references directly
        /// (<paramref name="memberValueRefProperty"/> is <c>null</c>) or contain a nested
        /// reference under the given property (e.g. <c>type</c>).
        /// </summary>
        private static void AddMapMemberEdges(
            List<TypeGraphEdge> edges, TypeGraphNode node, PackageDocument doc, JsonValueNode obj,
            string fieldName, string basePointer, TypeReferenceRole role, string? memberValueRefProperty)
        {
            if (!obj.TryGetProperty(fieldName, out var map) || map.Kind != JsonValueKind.Object) { return; }
            string mapPointer = basePointer + "/" + fieldName;

            foreach (var member in map.Properties)
            {
                string memberPointer = mapPointer + "/" + JsonPointerEscape(member.Name);

                if (memberValueRefProperty == null)
                {
                    if (TryParseReferenceObject(member.Value, doc, memberPointer, out var reference))
                    {
                        edges.Add(new TypeGraphEdge(node.Id, role, reference, member.Name));
                    }
                    continue;
                }

                if (member.Value.Kind != JsonValueKind.Object) { continue; }
                if (!member.Value.TryGetProperty(memberValueRefProperty, out var refValue)) { continue; }
                string refPointer = memberPointer + "/" + memberValueRefProperty;
                if (TryParseReferenceObject(refValue, doc, refPointer, out var typeReference))
                {
                    edges.Add(new TypeGraphEdge(node.Id, role, typeReference, member.Name));
                }
            }
        }

        /// <summary>Adds edges for an array-of-references field.</summary>
        private static void AddArrayMemberEdges(
            List<TypeGraphEdge> edges, TypeGraphNode node, PackageDocument doc, JsonValueNode obj,
            string fieldName, string basePointer, TypeReferenceRole role)
        {
            if (!obj.TryGetProperty(fieldName, out var array) || array.Kind != JsonValueKind.Array) { return; }
            string arrayPointer = basePointer + "/" + fieldName;

            for (int i = 0; i < array.Elements.Count; i++)
            {
                string pointer = arrayPointer + "/" + i;
                if (TryParseReferenceObject(array.Elements[i], doc, pointer, out var reference))
                {
                    edges.Add(new TypeGraphEdge(node.Id, role, reference, "[" + i + "]"));
                }
            }
        }

        /// <summary>Adds edges for an array-of-parameter-objects field, following each element's <c>type</c>.</summary>
        private static void AddParameterEdges(
            List<TypeGraphEdge> edges, TypeGraphNode node, PackageDocument doc, JsonValueNode obj,
            string fieldName, string basePointer, TypeReferenceRole role)
        {
            if (!obj.TryGetProperty(fieldName, out var array) || array.Kind != JsonValueKind.Array) { return; }
            string arrayPointer = basePointer + "/" + fieldName;

            for (int i = 0; i < array.Elements.Count; i++)
            {
                var element = array.Elements[i];
                if (element.Kind != JsonValueKind.Object) { continue; }
                if (!element.TryGetProperty("type", out var refValue)) { continue; }
                string pointer = arrayPointer + "/" + i + "/type";
                if (TryParseReferenceObject(refValue, doc, pointer, out var reference))
                {
                    edges.Add(new TypeGraphEdge(node.Id, role, reference, "[" + i + "]"));
                }
            }
        }

        // ── Reference parsing ────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to parse a <c>{"$ref": "..."}</c> object into a <see cref="ParsedTypeReference"/>.
        /// Returns <c>false</c> for anything that is not a well-formed, safe reference; such cases
        /// are already reported by the structural layer.
        /// </summary>
        private static bool TryParseReferenceObject(
            JsonValueNode node, PackageDocument sourceDocument, string refObjectPointer, out ParsedTypeReference reference)
        {
            reference = null!;

            if (node == null || node.Kind != JsonValueKind.Object) { return false; }
            if (!node.TryGetProperty("$ref", out var refNode) || refNode.Kind != JsonValueKind.String) { return false; }

            // A reference object must be canonical: exactly the '$ref' property and nothing
            // else. Extra properties are already reported by the structural layer (BCPVT014);
            // skipping such references here honors the recovery rule that malformed references
            // do not produce follow-on graph diagnostics.
            foreach (var property in node.Properties)
            {
                if (!string.Equals(property.Name, "$ref", StringComparison.Ordinal)) { return false; }
            }

            string rawText = refNode.StringValue ?? string.Empty;
            if (!ReferencePath.TryParse(rawText, out string packagePath, out int index)) { return false; }
            if (IsUnsafePackagePath(packagePath)) { return false; }

            string normalizedPath = packagePath.Length == 0
                ? string.Empty
                : DirectoryPackageFileSystem.NormalizeSeparators(packagePath);

            string sourcePointer = refObjectPointer + "/$ref";
            var loc = sourceDocument.SourceMap.GetLocation(refNode.ByteOffset);

            reference = new ParsedTypeReference(
                rawText,
                normalizedPath,
                index,
                sourceDocument.PackageRelativePath,
                sourcePointer,
                loc);
            return true;
        }

        /// <summary>
        /// Mirrors the structural layer's safe-path check: rejects rooted paths and any
        /// <c>..</c> traversal segment so graph resolution never escapes the package root.
        /// </summary>
        private static bool IsUnsafePackagePath(string packagePath)
        {
            if (string.IsNullOrEmpty(packagePath)) { return false; }

            string normalized = packagePath.Replace('\\', '/');

            if (normalized.StartsWith("/", StringComparison.Ordinal)) { return true; }
            if (normalized.Length >= 2 && normalized[1] == ':') { return true; }

            foreach (string segment in normalized.Split('/'))
            {
                if (segment == "..") { return true; }
            }
            return false;
        }

        private static string JsonPointerEscape(string token)
        {
            return token.Replace("~", "~0").Replace("/", "~1");
        }
    }
}
