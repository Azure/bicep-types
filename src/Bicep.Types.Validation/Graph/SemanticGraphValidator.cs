// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// Validates the package's semantic reference graph: resolves every reachable reference,
    /// reports unresolvable or mis-targeted references, and traverses the reachable closure of
    /// type objects exactly once.  Traversal is iterative (explicit stack) so deeply nested or
    /// cyclic graphs cannot overflow the call stack.
    /// </summary>
    internal static class SemanticGraphValidator
    {
        public static IReadOnlyList<TypeValidationDiagnostic> Validate(
            PackageDocumentProvider provider,
            PackageDocument indexDocument,
            TypePackageValidationOptions options)
        {
            if (provider == null) { throw new ArgumentNullException(nameof(provider)); }
            if (indexDocument == null) { throw new ArgumentNullException(nameof(indexDocument)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }

            var diagnostics = new List<TypeValidationDiagnostic>();
            var resolver = new TypeReferenceResolver(provider);
            var visited = new HashSet<TypeNodeId>();
            var stack = new Stack<TypeGraphNode>();

            // Seed traversal from index.json roots.
            foreach (var root in TypeGraphBuilder.ExtractRoots(indexDocument))
            {
                var resolution = resolver.Resolve(root.Reference);
                var target = HandleResolution(root.Reference, root.Role, root.Description, resolution, diagnostics);
                if (target != null && visited.Add(target.Id))
                {
                    stack.Push(target);
                }
            }

            // Depth-first traversal of the reachable closure. A node is enqueued once (visited
            // set), but every edge that targets it still gets an independent kind check.
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                foreach (var edge in TypeGraphBuilder.ExtractEdges(node))
                {
                    var resolution = resolver.Resolve(edge.Reference);
                    var target = HandleResolution(edge.Reference, edge.Role, rootDescription: null, resolution, diagnostics);
                    if (target != null && visited.Add(target.Id))
                    {
                        stack.Push(target);
                    }
                }
            }

            // Include structural/parse diagnostics accumulated while loading type files.
            diagnostics.AddRange(provider.LoadDiagnostics);
            return diagnostics;
        }

        /// <summary>
        /// Emits a diagnostic for a non-resolving or mis-targeted reference and returns the resolved
        /// target node (for traversal) when the reference resolved to a usable type object.
        /// </summary>
        private static TypeGraphNode? HandleResolution(
            ParsedTypeReference reference,
            TypeReferenceRole role,
            string? rootDescription,
            TypeReferenceResolution resolution,
            List<TypeValidationDiagnostic> diagnostics)
        {
            int line = reference.Location.Line;
            int column = reference.Location.Column;

            switch (resolution.Outcome)
            {
                case TypeReferenceResolutionOutcome.MissingFile:
                    diagnostics.Add(TypeValidationDiagnosticBuilder.ReferencedTypeFileMissing(
                        reference.SourcePackageRelativePath, reference.SourceJsonPointer, resolution.TargetPath, line, column));
                    return null;

                case TypeReferenceResolutionOutcome.FileReadFailed:
                    diagnostics.Add(TypeValidationDiagnosticBuilder.ReferencedTypeFileReadFailed(
                        reference.SourcePackageRelativePath, reference.SourceJsonPointer, resolution.TargetPath,
                        resolution.ReadError ?? string.Empty, line, column));
                    return null;

                case TypeReferenceResolutionOutcome.FileUnusable:
                    diagnostics.Add(TypeValidationDiagnosticBuilder.ReferencedTypeFileUnusable(
                        reference.SourcePackageRelativePath, reference.SourceJsonPointer, resolution.TargetPath, line, column));
                    return null;

                case TypeReferenceResolutionOutcome.IndexOutOfRange:
                    diagnostics.Add(TypeValidationDiagnosticBuilder.ReferenceIndexOutOfRange(
                        reference.SourcePackageRelativePath, reference.SourceJsonPointer, resolution.TargetPath,
                        reference.Index, resolution.TargetElementCount, line, column));
                    return null;

                case TypeReferenceResolutionOutcome.TargetNotTypeObject:
                    // The structural layer already reported that this element is not a usable
                    // type object; no additional graph diagnostic is emitted.
                    return null;

                case TypeReferenceResolutionOutcome.Resolved:
                    var node = resolution.TargetNode!;
                    if (!TypeTargetKindValidator.IsAllowed(role, node.Discriminator))
                    {
                        var related = new[]
                        {
                            new TypeValidationDiagnosticRelatedLocation(
                                "Target type is declared here.",
                                node.Id.PackageRelativePath,
                                node.JsonPointer,
                                node.Location.Line,
                                node.Location.Column),
                        };

                        string expected = TypeTargetKindValidator.ExpectedText(role);
                        string actual = $"'{node.Discriminator}'";

                        if (TypeTargetKindValidator.IsTopLevel(role))
                        {
                            diagnostics.Add(TypeValidationDiagnosticBuilder.TopLevelTargetKindMismatch(
                                reference.SourcePackageRelativePath, reference.SourceJsonPointer,
                                rootDescription ?? "This entry", expected, actual, line, column, related));
                        }
                        else
                        {
                            diagnostics.Add(TypeValidationDiagnosticBuilder.NestedTargetKindMismatch(
                                reference.SourcePackageRelativePath, reference.SourceJsonPointer,
                                RoleDescription(role), expected, actual, line, column, related));
                        }

                        // Recovery: a wrong-kind target is not traversed. A wrong root kind
                        // continues to the next root; a wrong nested kind does not descend
                        // through this edge. This prevents follow-on diagnostics rooted at a
                        // target whose kind is already known to be invalid.
                        return null;
                    }
                    return node;

                default:
                    return null;
            }
        }

        private static string RoleDescription(TypeReferenceRole role)
        {
            switch (role)
            {
                case TypeReferenceRole.ResourceBody: return "resource body";
                case TypeReferenceRole.ObjectPropertyType: return "object property type";
                case TypeReferenceRole.AdditionalProperties: return "additional properties type";
                case TypeReferenceRole.ArrayItem: return "array item type";
                case TypeReferenceRole.UnionMember: return "union member";
                case TypeReferenceRole.FunctionParameter: return "function parameter type";
                case TypeReferenceRole.FunctionOutput: return "function output type";
                case TypeReferenceRole.ResourceFunctionInput: return "resource function input type";
                case TypeReferenceRole.ResourceFunctionOutput: return "resource function output type";
                case TypeReferenceRole.NamespaceFunctionParameter: return "namespace function parameter type";
                case TypeReferenceRole.NamespaceFunctionOutput: return "namespace function output type";
                case TypeReferenceRole.ResourceTypeFunction: return "resource type function";
                case TypeReferenceRole.DiscriminatedObjectElement: return "discriminated object element";
                default: return "reference";
            }
        }
    }
}
