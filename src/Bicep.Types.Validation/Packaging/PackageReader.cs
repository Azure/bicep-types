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
    /// package root and parsing the index document. Type files are not loaded here; they are
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

            if (!TryOpenFileSystem(resolution, options, diagnostics, out IPackageFileSystem? fs))
            {
                return Fatal(diagnostics);
            }

            // The index document is always at the package-relative path "index.json".
            // For directory inputs, the file must exist at packageRoot/index.json.
            // For indexFile inputs, the resolution already computed the root as the containing directory, so the file is also at packageRoot/index.json.
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
            TypePackageValidationOptions options,
            List<TypeValidationDiagnostic> diagnostics,
            out IPackageFileSystem? fileSystem)
        {
            fileSystem = null;

            if (resolution.Kind == PackageInputKind.ArchiveFile || resolution.Kind == PackageInputKind.ArchiveStream)
            {
                return TryOpenArchiveFileSystem(resolution, options.ArchiveLimits, diagnostics, out fileSystem);
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

        private static bool TryOpenArchiveFileSystem(
            PackageInputResolution resolution,
            TypePackageArchiveLimits limits,
            List<TypeValidationDiagnostic> diagnostics,
            out IPackageFileSystem? fileSystem)
        {
            fileSystem = null;

            if (resolution.Kind == PackageInputKind.ArchiveStream)
            {
                if (resolution.ArchiveStream == null)
                {
                    diagnostics.Add(TypeValidationDiagnosticBuilder.ArchivePackageInvalid(
                        resolution.DisplayPath,
                        "the archive stream is unavailable"));
                    return false;
                }

                return TryCreateArchiveFileSystem(
                    resolution.ArchiveStream,
                    resolution.DisplayPath,
                    limits,
                    diagnostics,
                    out fileSystem);
            }

            var path = resolution.ArchiveFilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.PackagePathInvalid(resolution.DisplayPath));
                return false;
            }

            try
            {
                var fileInfo = new FileInfo(path!);
                if (fileInfo.Length > limits.MaxCompressedArchiveBytes)
                {
                    diagnostics.Add(TypeValidationDiagnosticBuilder.ArchivePackageInvalid(
                        resolution.DisplayPath,
                        $"the compressed archive exceeds the configured limit of {limits.MaxCompressedArchiveBytes} bytes"));
                    return false;
                }

                using var archiveStream = File.OpenRead(path!);
                return TryCreateArchiveFileSystem(
                    archiveStream,
                    resolution.DisplayPath,
                    limits,
                    diagnostics,
                    out fileSystem);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.PackageFileReadFailed(resolution.DisplayPath, ex.Message));
                return false;
            }
        }

        private static bool TryCreateArchiveFileSystem(
            Stream archiveStream,
            string displayPath,
            TypePackageArchiveLimits limits,
            List<TypeValidationDiagnostic> diagnostics,
            out IPackageFileSystem? fileSystem)
        {
            var archiveFileSystem = ArchivePackageFileSystem.Create(archiveStream, displayPath, limits);
            if (archiveFileSystem.HasFatalContainerFailure)
            {
                diagnostics.AddRange(archiveFileSystem.Diagnostics);
                fileSystem = null;
                return false;
            }

            fileSystem = archiveFileSystem;
            return true;
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
