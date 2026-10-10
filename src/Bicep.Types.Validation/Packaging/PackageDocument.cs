// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// One parsed package JSON document.  Carries the package-relative path, kind,
    /// source-mapped value tree, and optionally the physical path for debug use.
    /// </summary>
    internal sealed class PackageDocument
    {
        public PackageDocument(
            string packageRelativePath,
            PackageDocumentKind kind,
            JsonValueNode root,
            SourceMap sourceMap,
            string? physicalPath = null)
        {
            PackageRelativePath = packageRelativePath ?? throw new ArgumentNullException(nameof(packageRelativePath));
            Kind = kind;
            Root = root ?? throw new ArgumentNullException(nameof(root));
            SourceMap = sourceMap ?? throw new ArgumentNullException(nameof(sourceMap));
            PhysicalPath = physicalPath;
        }

        /// <summary>Package-relative path using <c>/</c> separators.</summary>
        public string PackageRelativePath { get; }

        /// <summary>Whether this is the index or a type file.</summary>
        public PackageDocumentKind Kind { get; }

        /// <summary>Parsed JSON value tree with source offsets.</summary>
        public JsonValueNode Root { get; }

        /// <summary>Source-location map for this document's bytes.</summary>
        public SourceMap SourceMap { get; }

        /// <summary>Physical file path, present only for file-backed documents; omit from diagnostics.</summary>
        public string? PhysicalPath { get; }
    }
}
