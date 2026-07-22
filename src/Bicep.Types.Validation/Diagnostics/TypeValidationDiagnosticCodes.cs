// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Diagnostics
{
    /// <summary>
    /// Stable diagnostic code constants. Codes are plain strings and form the
    /// user-facing and baseline-facing contract for validation diagnostics.
    /// </summary>
    public static class TypeValidationDiagnosticCodes
    {
        /// <summary>
        /// Reported when an archive (types.tgz) input is supplied, which is part of
        /// the public API but not implemented in phase 1.
        /// </summary>
        public const string ArchiveValidationNotImplemented = "BCPVT001";
    }
}
