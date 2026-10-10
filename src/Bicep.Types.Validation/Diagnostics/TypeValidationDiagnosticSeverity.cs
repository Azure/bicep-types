// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Diagnostics
{
    /// <summary>
    /// Severity of a single validation diagnostic.
    /// </summary>
    public enum TypeValidationDiagnosticSeverity
    {
        /// <summary>
        /// A problem that makes the package invalid.
        /// </summary>
        Error,

        /// <summary>
        /// A tolerated concern that does not make the package invalid.
        /// </summary>
        Warning,

        /// <summary>
        /// Informational output that is suppressed by default.
        /// </summary>
        Info,
    }
}
