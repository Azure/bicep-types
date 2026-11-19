// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Resource limits applied while reading a gzip-compressed tar type package.
    /// </summary>
    public sealed class TypePackageArchiveLimits
    {
        private const long Mebibyte = 1024L * 1024L;
        private const long DefaultMaxCompressedArchiveBytes = 64L * Mebibyte;
        private const long DefaultMaxExpandedArchiveBytes = 256L * Mebibyte;
        private const long DefaultMaxPackageFileBytes = 32L * Mebibyte;
        private const int DefaultMaxPackageFileCount = 4096;

        /// <summary>
        /// Creates archive limits. Every limit must be positive.
        /// </summary>
        public TypePackageArchiveLimits(
            long maxCompressedArchiveBytes = DefaultMaxCompressedArchiveBytes,
            long maxExpandedArchiveBytes = DefaultMaxExpandedArchiveBytes,
            long maxPackageFileBytes = DefaultMaxPackageFileBytes,
            int maxPackageFileCount = DefaultMaxPackageFileCount)
        {
            if (maxCompressedArchiveBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCompressedArchiveBytes), maxCompressedArchiveBytes, "The compressed archive byte limit must be positive.");
            }

            if (maxExpandedArchiveBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExpandedArchiveBytes), maxExpandedArchiveBytes, "The expanded archive byte limit must be positive.");
            }

            if (maxPackageFileBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPackageFileBytes), maxPackageFileBytes, "The package file byte limit must be positive.");
            }

            if (maxPackageFileCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPackageFileCount), maxPackageFileCount, "The package file count limit must be positive.");
            }

            MaxCompressedArchiveBytes = maxCompressedArchiveBytes;
            MaxExpandedArchiveBytes = maxExpandedArchiveBytes;
            MaxPackageFileBytes = maxPackageFileBytes;
            MaxPackageFileCount = maxPackageFileCount;
        }

        /// <summary>Default limits suitable for ordinary Bicep type packages.</summary>
        public static TypePackageArchiveLimits Default { get; } = new TypePackageArchiveLimits();

        /// <summary>Maximum number of gzip-compressed bytes that may be read. Defaults to 64 MiB.</summary>
        public long MaxCompressedArchiveBytes { get; }

        /// <summary>Maximum number of bytes the gzip stream may expand to. Defaults to 256 MiB.</summary>
        public long MaxExpandedArchiveBytes { get; }

        /// <summary>Maximum payload size of one package file. Defaults to 32 MiB.</summary>
        public long MaxPackageFileBytes { get; }

        /// <summary>Maximum number of package files in the archive. Defaults to 4096.</summary>
        public int MaxPackageFileCount { get; }
    }
}