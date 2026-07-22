// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;

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
            IReadOnlyList<TypeValidationDiagnostic> diagnostics)
        {
            Kind = kind;
            DisplayPath = displayPath;
            PackageRootPath = packageRootPath;
            IndexFilePath = indexFilePath;
            Diagnostics = diagnostics;
        }

        public PackageInputKind Kind { get; }

        public string DisplayPath { get; }

        /// <summary>Package root, for directory and index-file inputs.</summary>
        public string? PackageRootPath { get; }

        /// <summary>Index file path, for raw index inputs.</summary>
        public string? IndexFilePath { get; }

        /// <summary>Diagnostics produced while resolving the input.</summary>
        public IReadOnlyList<TypeValidationDiagnostic> Diagnostics { get; }
    }
}
