// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Internal result from <see cref="PackageReader"/>.
    /// </summary>
    internal sealed class PackageReadResult
    {
        public PackageReadResult(
            JsonDocumentSet documents,
            IReadOnlyList<TypeValidationDiagnostic> diagnostics,
            bool hasFatalReadFailure,
            IPackageFileSystem? fileSystem)
        {
            Documents = documents;
            Diagnostics = diagnostics;
            HasFatalReadFailure = hasFatalReadFailure;
            FileSystem = fileSystem;
        }

        /// <summary>All parsed package documents.</summary>
        public JsonDocumentSet Documents { get; }

        /// <summary>Diagnostics produced during package reading (IO and parse errors).</summary>
        public IReadOnlyList<TypeValidationDiagnostic> Diagnostics { get; }

        /// <summary>
        /// <c>true</c> when the index document is unavailable (missing or unparseable),
        /// indicating that structural validation should not proceed.
        /// </summary>
        public bool HasFatalReadFailure { get; }

        /// <summary>
        /// File system rooted at the package, used by the graph layer to load type files on
        /// demand.  <c>null</c> when reading failed before a package root was established.
        /// </summary>
        public IPackageFileSystem? FileSystem { get; }
    }
}
