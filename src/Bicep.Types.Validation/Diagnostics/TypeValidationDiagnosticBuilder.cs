// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation.Diagnostics
{
    /// <summary>
    /// Factory for validation diagnostics with stable codes and messages.
    /// </summary>
    public static class TypeValidationDiagnosticBuilder
    {
        /// <summary>
        /// Builds the deterministic diagnostic returned for archive inputs, which are
        /// part of the public API but not implemented in phase 1.
        /// </summary>
        /// <param name="displayPath">The caller-provided display path of the archive input.</param>
        public static TypeValidationDiagnostic ArchiveValidationNotImplemented(string displayPath)
        {
            if (displayPath is null)
            {
                throw new ArgumentNullException(nameof(displayPath));
            }

            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.ArchiveValidationNotImplemented,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Archive package validation is not implemented yet. Provide an extracted package directory or an 'index.json' file instead of archive input '{displayPath}'.");
        }
    }
}
