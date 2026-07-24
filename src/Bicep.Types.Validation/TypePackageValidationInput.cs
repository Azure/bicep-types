// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Discriminated public input model describing where a type package is located.
    /// </summary>
    /// <remarks>
    /// The hierarchy is closed: instances are created through the static factory methods.
    /// Directory, raw <c>index.json</c>, and gzip-compressed tar archive inputs are all validated.
    /// </remarks>
    public abstract class TypePackageValidationInput
    {
        private protected TypePackageValidationInput(string displayPath)
        {
            DisplayPath = displayPath ?? throw new ArgumentNullException(nameof(displayPath));
        }

        /// <summary>Display path preserved for diagnostics and result output.</summary>
        public string DisplayPath { get; }

        internal abstract PackageInputKind Kind { get; }

        /// <summary>Creates an input pointing at an extracted package directory.</summary>
        public static TypePackageValidationInput ForDirectory(string path) => new DirectoryValidationInput(path);

        /// <summary>Creates an input pointing at a raw <c>index.json</c> file.</summary>
        public static TypePackageValidationInput ForIndexFile(string path) => new IndexFileValidationInput(path);

        /// <summary>Creates an input pointing at a <c>types.tgz</c> archive file.</summary>
        public static TypePackageValidationInput ForArchiveFile(string path) => new ArchiveFileValidationInput(path);

        /// <summary>Creates an input reading a <c>types.tgz</c> archive from a stream.</summary>
        public static TypePackageValidationInput ForArchiveStream(Stream content, string displayPath) =>
            new ArchiveStreamValidationInput(content, displayPath);
    }

    internal sealed class DirectoryValidationInput : TypePackageValidationInput
    {
        public DirectoryValidationInput(string path)
            : base(path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Path { get; }

        internal override PackageInputKind Kind => PackageInputKind.Directory;
    }

    internal sealed class IndexFileValidationInput : TypePackageValidationInput
    {
        public IndexFileValidationInput(string path)
            : base(path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Path { get; }

        internal override PackageInputKind Kind => PackageInputKind.IndexFile;
    }

    internal sealed class ArchiveFileValidationInput : TypePackageValidationInput
    {
        public ArchiveFileValidationInput(string path)
            : base(path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Path { get; }

        internal override PackageInputKind Kind => PackageInputKind.ArchiveFile;
    }

    internal sealed class ArchiveStreamValidationInput : TypePackageValidationInput
    {
        public ArchiveStreamValidationInput(Stream content, string displayPath)
            : base(displayPath)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public Stream Content { get; }

        internal override PackageInputKind Kind => PackageInputKind.ArchiveStream;
    }
}
