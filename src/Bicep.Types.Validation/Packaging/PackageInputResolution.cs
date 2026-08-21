// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// The first normalized shape produced from a public validation input.
    /// </summary>
    internal sealed class PackageInputResolution
    {
        public PackageInputResolution(
            PackageInputKind kind,
            string displayPath,
            string? packageRootPath,
            string? indexFilePath,
            string? archiveFilePath = null,
            byte[]? archiveBytes = null)
        {
            Kind = kind;
            DisplayPath = displayPath;
            PackageRootPath = packageRootPath;
            IndexFilePath = indexFilePath;
            ArchiveFilePath = archiveFilePath;
            ArchiveBytes = archiveBytes;
        }

        public PackageInputKind Kind { get; }

        public string DisplayPath { get; }

        /// <summary>Package root, for directory and index-file inputs.</summary>
        public string? PackageRootPath { get; }

        /// <summary>Index file path, for raw index inputs.</summary>
        public string? IndexFilePath { get; }

        /// <summary>Physical archive path, for archive-file inputs.</summary>
        public string? ArchiveFilePath { get; }

        /// <summary>Archive bytes read fully into memory, for archive-stream inputs.</summary>
        public byte[]? ArchiveBytes { get; }
    }
}
