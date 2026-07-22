// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Options controlling a validation run.
    /// </summary>
    public sealed class TypePackageValidationOptions
    {
        private int? maxDiagnostics;

        /// <summary>Validation mode. Defaults to <see cref="TypePackageValidationMode.CanonicalWriter"/>.</summary>
        public TypePackageValidationMode Mode { get; set; } = TypePackageValidationMode.CanonicalWriter;

        /// <summary>Whether warning diagnostics are returned. Defaults to <c>true</c>.</summary>
        public bool IncludeWarnings { get; set; } = true;

        /// <summary>Whether informational diagnostics are returned. Defaults to <c>false</c>.</summary>
        public bool IncludeInformationalDiagnostics { get; set; }

        /// <summary>Whether files unreachable from graph roots are validated. Defaults to <c>false</c>.</summary>
        public bool ValidateUnreachableFiles { get; set; }

        /// <summary>
        /// Optional cap on the number of returned diagnostics. <c>null</c> means no cap.
        /// Truncation occurs only when this is a positive value and the number of
        /// diagnostics to return exceeds it. Negative values are rejected.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to a negative value.</exception>
        public int? MaxDiagnostics
        {
            get => maxDiagnostics;
            set
            {
                if (value.HasValue && value.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "MaxDiagnostics must be null (no cap) or a non-negative value.");
                }

                maxDiagnostics = value;
            }
        }
    }
}
