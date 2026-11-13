// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

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
            Stream? archiveStream = null)
        {
            Kind = kind;
            DisplayPath = displayPath;
            PackageRootPath = packageRootPath;
            IndexFilePath = indexFilePath;
            ArchiveFilePath = archiveFilePath;
            ArchiveStream = archiveStream;
        }

        public PackageInputKind Kind { get; }

        public string DisplayPath { get; }

        /// <summary>Package root, for directory and index-file inputs.</summary>
        public string? PackageRootPath { get; }

        /// <summary>Index file path, for raw index inputs.</summary>
        public string? IndexFilePath { get; }

        /// <summary>Physical archive path, for archive-file inputs.</summary>
        public string? ArchiveFilePath { get; }

        /// <summary>Caller-owned content stream, for archive-stream inputs.</summary>
        public Stream? ArchiveStream { get; }
    }
}
