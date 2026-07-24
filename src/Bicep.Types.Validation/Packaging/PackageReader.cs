// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Azure.Bicep.Types.Validation.Diagnostics;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Reads a package's <c>index.json</c> into a <see cref="PackageReadResult"/> by resolving the
    /// package root and parsing the index document.  Type files are not loaded here; they are
    /// loaded on demand by the graph layer's package document provider, which owns transitive
    /// closure and per-file structural validation.
    /// </summary>
    internal static class PackageReader
    {
        private const string IndexFileName = "index.json";

        public static PackageReadResult Read(PackageInputResolution resolution, TypePackageValidationOptions options)
        {
            if (resolution == null) { throw new ArgumentNullException(nameof(resolution)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }

            // Archive inputs never reach here – they carry the BCPVT001 diagnostic from
            // PackageInputResolver and TypePackageValidator returns before calling Read.
            var diagnostics = new List<TypeValidationDiagnostic>();

            // Resolve the physical package root
            string? packageRoot = resolution.PackageRootPath;
            if (string.IsNullOrEmpty(packageRoot))
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.PackagePathInvalid(resolution.DisplayPath));
                return Fatal(diagnostics);
            }

            if (!Directory.Exists(packageRoot))
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.PackagePathInvalid(resolution.DisplayPath));
                return Fatal(diagnostics);
            }

            var fs = new DirectoryPackageFileSystem(packageRoot!);

            // The index document is always at the package-relative path "index.json".
            // For directory inputs, the file must exist at packageRoot/index.json.
            // For indexFile inputs, the resolution already computed the root as the
            // containing directory, so the file is also at packageRoot/index.json.
            if (!fs.FileExists(IndexFileName))
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.IndexFileMissing(resolution.DisplayPath));
                return Fatal(diagnostics);
            }

            // Read index.json bytes
            if (!fs.TryReadAllBytes(IndexFileName, out byte[] indexBytes, out string indexReadError))
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.PackageFileReadFailed(IndexFileName, indexReadError));
                return Fatal(diagnostics);
            }

            // Parse index.json
            if (!SourceMap.TryParse(indexBytes, IndexFileName, out JsonValueNode? indexRoot, out SourceMap indexSourceMap, out var indexParseError))
            {
                var err = indexParseError!.Value;
                diagnostics.Add(TypeValidationDiagnosticBuilder.JsonSyntaxInvalid(IndexFileName, err.line, err.column, err.message));
                return Fatal(diagnostics);
            }

            var indexDoc = new PackageDocument(IndexFileName, PackageDocumentKind.Index, indexRoot!, indexSourceMap);

            // Type files are loaded lazily by the graph layer's provider; only the index
            // document is materialized here.
            var documents = new JsonDocumentSet(indexDoc, new PackageDocument[0]);
            return new PackageReadResult(documents, diagnostics, hasFatalReadFailure: false, fileSystem: fs);
        }

        private static PackageReadResult Fatal(List<TypeValidationDiagnostic> diagnostics)
        {
            return new PackageReadResult(
                new JsonDocumentSet(null, new PackageDocument[0]),
                diagnostics,
                hasFatalReadFailure: true,
                fileSystem: null);
        }
    }
}
