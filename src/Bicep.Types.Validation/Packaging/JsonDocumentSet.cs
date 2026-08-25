// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Immutable, ordered collection of parsed package documents.
    /// Documents are keyed by their package-relative path (forward-slash normalized).
    /// Duplicate paths are rejected at construction time.
    /// </summary>
    internal sealed class JsonDocumentSet
    {
        private readonly Dictionary<string, PackageDocument> byPath;
        private readonly List<PackageDocument> typeFiles;

        public JsonDocumentSet(PackageDocument? indexDocument, IEnumerable<PackageDocument> typeFiles)
        {
            this.byPath = new Dictionary<string, PackageDocument>(StringComparer.OrdinalIgnoreCase);
            this.typeFiles = new List<PackageDocument>();

            IndexDocument = indexDocument;

            if (indexDocument != null)
            {
                this.byPath[indexDocument.PackageRelativePath] = indexDocument;
            }

            if (typeFiles != null)
            {
                foreach (var doc in typeFiles)
                {
                    if (!this.byPath.ContainsKey(doc.PackageRelativePath))
                    {
                        this.byPath[doc.PackageRelativePath] = doc;
                        this.typeFiles.Add(doc);
                    }
                }
            }
        }

        /// <summary>The index document, or <c>null</c> if reading failed.</summary>
        public PackageDocument? IndexDocument { get; }

        /// <summary>Type-file documents in deterministic (load-discovery) order.</summary>
        public IReadOnlyList<PackageDocument> TypeFiles => typeFiles;

        /// <summary>Looks up a document by package-relative path.  Returns <c>null</c> if not found.</summary>
        public PackageDocument? TryGetDocument(string packageRelativePath)
        {
            return this.byPath.TryGetValue(packageRelativePath, out var doc) ? doc : null;
        }
    }
}
