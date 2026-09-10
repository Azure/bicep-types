// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Classifies a public validation input into its first normalized shape.
    /// </summary>
    /// <remarks>
    /// The resolver records the input shape that <see cref="PackageReader"/> extends.  Directory and
    /// index-file inputs carry a package root; archive inputs carry the archive path or the caller's
    /// archive stream without eagerly buffering its content.
    /// </remarks>
    internal static class PackageInputResolver
    {
        public static PackageInputResolution Resolve(TypePackageValidationInput input)
        {
            switch (input)
            {
                case DirectoryValidationInput directory:
                    return new PackageInputResolution(
                        PackageInputKind.Directory,
                        directory.DisplayPath,
                        packageRootPath: directory.Path,
                        indexFilePath: null);

                case IndexFileValidationInput index:
                    var root = Path.GetDirectoryName(index.Path);
                    return new PackageInputResolution(
                        PackageInputKind.IndexFile,
                        index.DisplayPath,
                        packageRootPath: string.IsNullOrEmpty(root) ? "." : root,
                        indexFilePath: index.Path);

                case ArchiveFileValidationInput archiveFile:
                    return new PackageInputResolution(
                        PackageInputKind.ArchiveFile,
                        archiveFile.DisplayPath,
                        packageRootPath: null,
                        indexFilePath: null,
                        archiveFilePath: archiveFile.Path);

                case ArchiveStreamValidationInput archiveStream:
                    return new PackageInputResolution(
                        PackageInputKind.ArchiveStream,
                        archiveStream.DisplayPath,
                        packageRootPath: null,
                        indexFilePath: null,
                        archiveStream: archiveStream.Content);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(input),
                        input?.GetType().FullName,
                        "Unsupported validation input type.");
            }
        }

    }
}
