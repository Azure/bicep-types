// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>Validates a parsed reference form against its source document kind.</summary>
    internal static class ReferencePlacement
    {
        /// <summary>Returns whether the canonical target path is allowed in the source document.</summary>
        public static bool IsAllowed(PackageDocumentKind sourceDocumentKind, string canonicalPackagePath)
        {
            if (canonicalPackagePath == null) { throw new ArgumentNullException(nameof(canonicalPackagePath)); }

            switch (sourceDocumentKind)
            {
                case PackageDocumentKind.Index:
                    return canonicalPackagePath.Length > 0;
                case PackageDocumentKind.TypeFile:
                    return canonicalPackagePath.Length == 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sourceDocumentKind), sourceDocumentKind, "Unsupported package document kind.");
            }
        }
    }
}