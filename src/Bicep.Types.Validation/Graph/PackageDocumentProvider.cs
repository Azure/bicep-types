// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.Structural;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// Loads type files on demand for graph traversal.  Each file is read, parsed, structurally
    /// validated, and turned into graph nodes exactly once; subsequent lookups are served from a
    /// cache.  All diagnostics produced during loading are accumulated in <see cref="LoadDiagnostics"/>.
    /// </summary>
    internal sealed class PackageDocumentProvider
    {
        private readonly IPackageFileSystem fileSystem;
        private readonly PackageDocument indexDocument;
        private readonly string indexPath;
        private readonly TypePackageValidationOptions options;
        private readonly Dictionary<string, PackageDocumentProviderResult> cache =
            new Dictionary<string, PackageDocumentProviderResult>(StringComparer.OrdinalIgnoreCase);
        private readonly List<TypeValidationDiagnostic> loadDiagnostics = new List<TypeValidationDiagnostic>();

        public PackageDocumentProvider(
            IPackageFileSystem fileSystem,
            PackageDocument indexDocument,
            TypePackageValidationOptions options)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.indexDocument = indexDocument ?? throw new ArgumentNullException(nameof(indexDocument));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.indexPath = DirectoryPackageFileSystem.NormalizeSeparators(indexDocument.PackageRelativePath);
        }

        /// <summary>All diagnostics accumulated across loaded type files.</summary>
        public IReadOnlyList<TypeValidationDiagnostic> LoadDiagnostics => loadDiagnostics;

        /// <summary>
        /// Returns the type files that graph traversal loaded and that parsed into a usable
        /// type-file array.  Policy validation inspects every structurally usable element of
        /// these reached files (including elements at unreferenced indices), which is why this
        /// exposes the whole reached file rather than only graph-visited nodes.  The index
        /// document (served for same-file references) is excluded because its root is an object.
        /// </summary>
        public IEnumerable<PackageDocumentProviderResult> GetReachedUsableTypeFiles()
        {
            foreach (var result in cache.Values)
            {
                if (result.IsStructurallyUsable &&
                    result.Document != null &&
                    result.Document.Kind == PackageDocumentKind.TypeFile)
                {
                    yield return result;
                }
            }
        }

        /// <summary>Loads (or returns the cached result for) a type file by package-relative path.</summary>
        public PackageDocumentProviderResult GetTypeFile(string packageRelativePath)
        {
            if (packageRelativePath == null) { throw new ArgumentNullException(nameof(packageRelativePath)); }

            string key = DirectoryPackageFileSystem.NormalizeSeparators(packageRelativePath);
            if (cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var result = Load(key);
            cache[key] = result;
            if (result.Diagnostics.Count > 0)
            {
                loadDiagnostics.AddRange(result.Diagnostics);
            }
            return result;
        }

        private PackageDocumentProviderResult Load(string path)
        {
            // A reference that resolves to index.json (e.g. a same-file ref inside index) is
            // served from the already-parsed index document. Its root is an object, so it is
            // never a usable type-file array.
            if (string.Equals(path, indexPath, StringComparison.OrdinalIgnoreCase))
            {
                var indexNodes = TypeGraphBuilder.BuildNodes(indexDocument);
                return PackageDocumentProviderResult.Loaded(indexDocument, indexNodes != null, indexNodes, Array.Empty<TypeValidationDiagnostic>());
            }

            if (!fileSystem.FileExists(path))
            {
                return PackageDocumentProviderResult.Missing();
            }

            if (!fileSystem.TryReadAllBytes(path, out byte[] bytes, out string error))
            {
                return PackageDocumentProviderResult.ReadFailed(error);
            }

            if (!SourceMap.TryParse(bytes, path, out JsonValueNode? root, out SourceMap sourceMap, out var parseError))
            {
                var err = parseError!.Value;
                var diagnostic = TypeValidationDiagnosticBuilder.JsonSyntaxInvalid(path, err.line, err.column, err.message);
                return PackageDocumentProviderResult.ParseFailed(new[] { diagnostic });
            }

            var document = new PackageDocument(path, PackageDocumentKind.TypeFile, root!, sourceMap);

            // Structurally validate the type file exactly once, and build its graph nodes.
            var structuralDiagnostics = StructuralValidator.ValidateTypeFileDocument(document, options);
            var nodes = TypeGraphBuilder.BuildNodes(document);
            bool usable = nodes != null; // usable iff root is a type-file array

            return PackageDocumentProviderResult.Loaded(document, usable, nodes, structuralDiagnostics);
        }
    }
}
