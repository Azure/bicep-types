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

            var diagnostics = new List<TypeValidationDiagnostic>();

            if (!TryOpenFileSystem(resolution, diagnostics, out IPackageFileSystem? fs))
            {
                return Fatal(diagnostics);
            }

            // The index document is always at the package-relative path "index.json".
            // For directory inputs, the file must exist at packageRoot/index.json.
            // For indexFile inputs, the resolution already computed the root as the
            // containing directory, so the file is also at packageRoot/index.json.
            // For archive inputs, only the archive-root index.json is the package index.
            if (!fs!.FileExists(IndexFileName))
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

        /// <summary>
        /// Opens the appropriate <see cref="IPackageFileSystem"/> for the resolved input kind, adding a
        /// fatal diagnostic and returning <c>false</c> when the package container cannot be opened.
        /// </summary>
        private static bool TryOpenFileSystem(
            PackageInputResolution resolution,
            List<TypeValidationDiagnostic> diagnostics,
            out IPackageFileSystem? fileSystem)
        {
            fileSystem = null;

            if (resolution.Kind == PackageInputKind.ArchiveFile || resolution.Kind == PackageInputKind.ArchiveStream)
            {
                if (!TryGetArchiveBytes(resolution, diagnostics, out byte[]? archiveBytes))
                {
                    return false;
                }

                var archiveFs = ArchivePackageFileSystem.Create(archiveBytes!, resolution.DisplayPath);
                if (archiveFs.HasFatalContainerFailure)
                {
                    diagnostics.AddRange(archiveFs.Diagnostics);
                    return false;
                }

                fileSystem = archiveFs;
                return true;
            }

            string? packageRoot = resolution.PackageRootPath;
            if (string.IsNullOrEmpty(packageRoot) || !Directory.Exists(packageRoot))
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.PackagePathInvalid(resolution.DisplayPath));
                return false;
            }

            fileSystem = new DirectoryPackageFileSystem(packageRoot!);
            return true;
        }

        /// <summary>Resolves the raw archive bytes for an archive-file or archive-stream input.</summary>
        private static bool TryGetArchiveBytes(
            PackageInputResolution resolution,
            List<TypeValidationDiagnostic> diagnostics,
            out byte[]? archiveBytes)
        {
            if (resolution.Kind == PackageInputKind.ArchiveStream)
            {
                archiveBytes = resolution.ArchiveBytes ?? Array.Empty<byte>();
                return true;
            }

            archiveBytes = null;
            var path = resolution.ArchiveFilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.PackagePathInvalid(resolution.DisplayPath));
                return false;
            }

            try
            {
                archiveBytes = File.ReadAllBytes(path!);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.PackageFileReadFailed(resolution.DisplayPath, ex.Message));
                return false;
            }
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
