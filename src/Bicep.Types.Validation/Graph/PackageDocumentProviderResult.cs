// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// Outcome of a package document provider lookup for one type file.
    /// </summary>
    internal sealed class PackageDocumentProviderResult
    {
        private static readonly IReadOnlyList<TypeGraphNode?> NoNodes = new TypeGraphNode?[0];
        private static readonly IReadOnlyList<TypeValidationDiagnostic> NoDiagnostics = new TypeValidationDiagnostic[0];

        private PackageDocumentProviderResult(
            bool exists,
            bool isStructurallyUsable,
            string? readError,
            PackageDocument? document,
            IReadOnlyList<TypeGraphNode?> nodesByIndex,
            IReadOnlyList<TypeValidationDiagnostic> diagnostics)
        {
            Exists = exists;
            IsStructurallyUsable = isStructurallyUsable;
            ReadError = readError;
            Document = document;
            NodesByIndex = nodesByIndex;
            Diagnostics = diagnostics;
        }

        /// <summary><c>true</c> if the type file exists on disk.</summary>
        public bool Exists { get; }

        /// <summary><c>true</c> if the file parsed and its root is a type-file array.</summary>
        public bool IsStructurallyUsable { get; }

        /// <summary>IO error message when the file exists but could not be read; otherwise <c>null</c>.</summary>
        public string? ReadError { get; }

        /// <summary>The parsed document when available; otherwise <c>null</c>.</summary>
        public PackageDocument? Document { get; }

        /// <summary>Index-aligned graph nodes; <c>null</c> entries mark non-usable elements. Empty when not usable.</summary>
        public IReadOnlyList<TypeGraphNode?> NodesByIndex { get; }

        /// <summary>Structural or parse diagnostics produced while loading this file.</summary>
        public IReadOnlyList<TypeValidationDiagnostic> Diagnostics { get; }

        public static PackageDocumentProviderResult Missing() =>
            new PackageDocumentProviderResult(false, false, null, null, NoNodes, NoDiagnostics);

        public static PackageDocumentProviderResult ReadFailed(string readError) =>
            new PackageDocumentProviderResult(true, false, readError, null, NoNodes, NoDiagnostics);

        public static PackageDocumentProviderResult ParseFailed(IReadOnlyList<TypeValidationDiagnostic> diagnostics) =>
            new PackageDocumentProviderResult(true, false, null, null, NoNodes, diagnostics);

        public static PackageDocumentProviderResult Loaded(
            PackageDocument document,
            bool isStructurallyUsable,
            IReadOnlyList<TypeGraphNode?>? nodesByIndex,
            IReadOnlyList<TypeValidationDiagnostic> diagnostics) =>
            new PackageDocumentProviderResult(true, isStructurallyUsable, null, document, nodesByIndex ?? NoNodes, diagnostics);
    }
}
