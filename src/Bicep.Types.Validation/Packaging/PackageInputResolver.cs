// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Azure.Bicep.Types.Validation.Diagnostics;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Classifies a public validation input into its first normalized shape.
    /// </summary>
    /// <remarks>
    /// The resolver records the input shape that <see cref="PackageReader"/> extends.  Directory and
    /// index-file inputs carry a package root; archive inputs carry the archive path or the archive
    /// bytes read fully from the caller's stream.  Archive validity is decided later by the reader.
    /// </remarks>
    internal static class PackageInputResolver
    {
        private static readonly TypeValidationDiagnostic[] NoDiagnostics = new TypeValidationDiagnostic[0];

        public static PackageInputResolution Resolve(TypePackageValidationInput input)
        {
            switch (input)
            {
                case DirectoryValidationInput directory:
                    return new PackageInputResolution(
                        PackageInputKind.Directory,
                        directory.DisplayPath,
                        packageRootPath: directory.Path,
                        indexFilePath: null,
                        diagnostics: NoDiagnostics);

                case IndexFileValidationInput index:
                    var root = Path.GetDirectoryName(index.Path);
                    return new PackageInputResolution(
                        PackageInputKind.IndexFile,
                        index.DisplayPath,
                        packageRootPath: string.IsNullOrEmpty(root) ? "." : root,
                        indexFilePath: index.Path,
                        diagnostics: NoDiagnostics);

                case ArchiveFileValidationInput archiveFile:
                    return new PackageInputResolution(
                        PackageInputKind.ArchiveFile,
                        archiveFile.DisplayPath,
                        packageRootPath: null,
                        indexFilePath: null,
                        diagnostics: NoDiagnostics,
                        archiveFilePath: archiveFile.Path);

                case ArchiveStreamValidationInput archiveStream:
                    // Read the caller-provided stream fully into memory without disposing it and
                    // without requiring seekability.
                    var bytes = ReadStreamFully(archiveStream.Content);
                    return new PackageInputResolution(
                        PackageInputKind.ArchiveStream,
                        archiveStream.DisplayPath,
                        packageRootPath: null,
                        indexFilePath: null,
                        diagnostics: NoDiagnostics,
                        archiveBytes: bytes);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(input),
                        input?.GetType().FullName,
                        "Unsupported validation input type.");
            }
        }

        private static byte[] ReadStreamFully(Stream stream)
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
    }
}
