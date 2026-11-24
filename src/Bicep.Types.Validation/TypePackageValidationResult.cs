// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Bicep.Types.Validation.Diagnostics;

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Result of a validation run.
    /// </summary>
    public sealed class TypePackageValidationResult
    {
        private static readonly IReadOnlyList<TypeValidationDiagnostic> NoDiagnostics =
            new TypeValidationDiagnostic[0];

        public TypePackageValidationResult(
            bool isValid,
            TypePackageValidationMode mode,
            IReadOnlyList<TypeValidationDiagnostic> diagnostics,
            bool diagnosticsTruncated,
            TypePackageValidationSummary summary)
        {
            IsValid = isValid;
            Mode = mode;
            Diagnostics = diagnostics ?? NoDiagnostics;
            DiagnosticsTruncated = diagnosticsTruncated;
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        }

        /// <summary>
        /// Whether the package is valid. This is based on all detected error
        /// diagnostics and is independent of filtering and truncation.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>The mode the result was produced for.</summary>
        public TypePackageValidationMode Mode { get; }

        /// <summary>The returned diagnostics after filtering and truncation.</summary>
        public IReadOnlyList<TypeValidationDiagnostic> Diagnostics { get; }

        /// <summary>Whether the returned diagnostics were truncated.</summary>
        public bool DiagnosticsTruncated { get; }

        /// <summary>Counts across all detected diagnostics, before filtering and truncation.</summary>
        public TypePackageValidationSummary Summary { get; }

        /// <summary>
        /// Composes a result from the full set of detected diagnostics and the run options.
        /// Diagnostics are sorted deterministically; the summary counts all detected
        /// diagnostics; <see cref="IsValid"/> reflects any detected error; and filtering
        /// and truncation affect only the returned diagnostics list.
        /// </summary>
        public static TypePackageValidationResult Create(
            TypePackageValidationMode mode,
            IEnumerable<TypeValidationDiagnostic> detectedDiagnostics,
            TypePackageValidationOptions options)
        {
            if (detectedDiagnostics is null)
            {
                throw new ArgumentNullException(nameof(detectedDiagnostics));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var all = detectedDiagnostics.ToList();
            all.Sort(TypeValidationDiagnosticComparer.Instance);

            var errorCount = all.Count(d => d.Severity == TypeValidationDiagnosticSeverity.Error);
            var warningCount = all.Count(d => d.Severity == TypeValidationDiagnosticSeverity.Warning);
            var infoCount = all.Count(d => d.Severity == TypeValidationDiagnosticSeverity.Info);

            var isValid = errorCount == 0;

            IEnumerable<TypeValidationDiagnostic> filtered = all;
            if (!options.IncludeWarnings)
            {
                filtered = filtered.Where(d => d.Severity != TypeValidationDiagnosticSeverity.Warning);
            }

            if (!options.IncludeInformationalDiagnostics)
            {
                filtered = filtered.Where(d => d.Severity != TypeValidationDiagnosticSeverity.Info);
            }

            var returned = filtered.ToList();

            var truncated = false;
            if (options.MaxDiagnostics is int max && max > 0 && returned.Count > max)
            {
                returned = returned.Take(max).ToList();
                truncated = true;
            }

            var summary = new TypePackageValidationSummary(errorCount, warningCount, infoCount);
            return new TypePackageValidationResult(isValid, mode, returned, truncated, summary);
        }
    }
}
