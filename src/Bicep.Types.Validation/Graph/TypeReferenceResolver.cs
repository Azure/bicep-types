// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>Discriminated outcome of resolving a <see cref="ParsedTypeReference"/>.</summary>
    internal enum TypeReferenceResolutionOutcome
    {
        /// <summary>The reference resolved to a usable type object.</summary>
        Resolved,

        /// <summary>The target type file does not exist.</summary>
        MissingFile,

        /// <summary>The target type file exists but could not be read.</summary>
        FileReadFailed,

        /// <summary>The target type file could not be parsed or is not a type-file array.</summary>
        FileUnusable,

        /// <summary>The referenced index is out of range for the target file.</summary>
        IndexOutOfRange,

        /// <summary>The referenced element exists but is not a usable type object.</summary>
        TargetNotTypeObject,
    }

    /// <summary>The result of resolving a reference, including the target when available.</summary>
    internal sealed class TypeReferenceResolution
    {
        private TypeReferenceResolution(
            TypeReferenceResolutionOutcome outcome,
            string targetPath,
            TypeGraphNode? targetNode,
            int targetElementCount,
            string? readError)
        {
            Outcome = outcome;
            TargetPath = targetPath;
            TargetNode = targetNode;
            TargetElementCount = targetElementCount;
            ReadError = readError;
        }

        public TypeReferenceResolutionOutcome Outcome { get; }

        /// <summary>Effective target package-relative path used for resolution.</summary>
        public string TargetPath { get; }

        /// <summary>The resolved target node when <see cref="Outcome"/> is <see cref="TypeReferenceResolutionOutcome.Resolved"/>.</summary>
        public TypeGraphNode? TargetNode { get; }

        /// <summary>Number of type-object slots in the target file (for out-of-range messaging).</summary>
        public int TargetElementCount { get; }

        /// <summary>IO error text when <see cref="Outcome"/> is <see cref="TypeReferenceResolutionOutcome.FileReadFailed"/>.</summary>
        public string? ReadError { get; }

        public static TypeReferenceResolution Resolved(string targetPath, TypeGraphNode node, int count) =>
            new TypeReferenceResolution(TypeReferenceResolutionOutcome.Resolved, targetPath, node, count, null);

        public static TypeReferenceResolution Missing(string targetPath) =>
            new TypeReferenceResolution(TypeReferenceResolutionOutcome.MissingFile, targetPath, null, 0, null);

        public static TypeReferenceResolution ReadFailed(string targetPath, string readError) =>
            new TypeReferenceResolution(TypeReferenceResolutionOutcome.FileReadFailed, targetPath, null, 0, readError);

        public static TypeReferenceResolution Unusable(string targetPath) =>
            new TypeReferenceResolution(TypeReferenceResolutionOutcome.FileUnusable, targetPath, null, 0, null);

        public static TypeReferenceResolution OutOfRange(string targetPath, int count) =>
            new TypeReferenceResolution(TypeReferenceResolutionOutcome.IndexOutOfRange, targetPath, null, count, null);

        public static TypeReferenceResolution NotTypeObject(string targetPath, int count) =>
            new TypeReferenceResolution(TypeReferenceResolutionOutcome.TargetNotTypeObject, targetPath, null, count, null);
    }

    /// <summary>
    /// Resolves parsed references to target nodes using a <see cref="PackageDocumentProvider"/>.
    /// Pure with respect to diagnostics: the caller decides how to report each outcome, since
    /// target-kind expectations depend on the referencing role.
    /// </summary>
    internal sealed class TypeReferenceResolver
    {
        private readonly PackageDocumentProvider provider;

        public TypeReferenceResolver(PackageDocumentProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public TypeReferenceResolution Resolve(ParsedTypeReference reference)
        {
            if (reference == null) { throw new ArgumentNullException(nameof(reference)); }

            string targetPath = reference.EffectiveTargetPath;
            var file = provider.GetTypeFile(targetPath);

            if (!file.Exists)
            {
                return TypeReferenceResolution.Missing(targetPath);
            }

            if (file.ReadError != null)
            {
                return TypeReferenceResolution.ReadFailed(targetPath, file.ReadError);
            }

            if (!file.IsStructurallyUsable)
            {
                return TypeReferenceResolution.Unusable(targetPath);
            }

            int count = file.NodesByIndex.Count;
            if (reference.Index < 0 || reference.Index >= count)
            {
                return TypeReferenceResolution.OutOfRange(targetPath, count);
            }

            var node = file.NodesByIndex[reference.Index];
            if (node == null)
            {
                return TypeReferenceResolution.NotTypeObject(targetPath, count);
            }

            return TypeReferenceResolution.Resolved(targetPath, node, count);
        }
    }
}
