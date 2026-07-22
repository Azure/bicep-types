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
    /// Phase 1 does not read the file system. It records the shape that phase 2 will
    /// extend, and produces the deterministic not-implemented diagnostic for archives.
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
                    return ArchiveNotImplemented(PackageInputKind.ArchiveFile, archiveFile.DisplayPath);

                case ArchiveStreamValidationInput archiveStream:
                    return ArchiveNotImplemented(PackageInputKind.ArchiveStream, archiveStream.DisplayPath);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(input),
                        input?.GetType().FullName,
                        "Unsupported validation input type.");
            }
        }

        private static PackageInputResolution ArchiveNotImplemented(PackageInputKind kind, string displayPath)
        {
            var diagnostic = TypeValidationDiagnosticBuilder.ArchiveValidationNotImplemented(displayPath);
            return new PackageInputResolution(
                kind,
                displayPath,
                packageRootPath: null,
                indexFilePath: null,
                diagnostics: new[] { diagnostic });
        }
    }
}
