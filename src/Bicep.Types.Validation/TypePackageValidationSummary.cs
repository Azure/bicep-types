// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Diagnostic counts by severity across all detected diagnostics, before any
    /// filtering or truncation is applied.
    /// </summary>
    public sealed class TypePackageValidationSummary
    {
        public TypePackageValidationSummary(int errorCount, int warningCount, int infoCount)
        {
            ErrorCount = errorCount;
            WarningCount = warningCount;
            InfoCount = infoCount;
        }

        /// <summary>Total number of detected error diagnostics.</summary>
        public int ErrorCount { get; }

        /// <summary>Total number of detected warning diagnostics.</summary>
        public int WarningCount { get; }

        /// <summary>Total number of detected informational diagnostics.</summary>
        public int InfoCount { get; }
    }
}
