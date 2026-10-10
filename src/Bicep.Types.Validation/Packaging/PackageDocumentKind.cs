// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>Kind of a parsed package JSON document.</summary>
    internal enum PackageDocumentKind
    {
        /// <summary>The <c>index.json</c> file at the package root.</summary>
        Index,

        /// <summary>A type file referenced from <c>index.json</c>.</summary>
        TypeFile,
    }
}
