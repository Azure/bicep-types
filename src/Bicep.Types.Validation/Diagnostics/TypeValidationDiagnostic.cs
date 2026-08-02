// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Diagnostics
{
    /// <summary>
    /// A single structured validation diagnostic.
    /// </summary>
    /// <remarks>
    /// Phase 1 uses flat location fields (<see cref="Path"/>, <see cref="JsonPointer"/>,
    /// <see cref="Line"/>, <see cref="Column"/>) instead of a source span, and omits any
    /// rule identifier. Location fields may be absent for input-level diagnostics that do
    /// not point into package JSON.
    /// </remarks>
    public sealed class TypeValidationDiagnostic
    {
        private static readonly IReadOnlyList<TypeValidationDiagnosticRelatedLocation> NoRelatedLocations =
            new TypeValidationDiagnosticRelatedLocation[0];

        public TypeValidationDiagnostic(
            string code,
            TypeValidationDiagnosticSeverity severity,
            string message,
            string? path = null,
            string? jsonPointer = null,
            int? line = null,
            int? column = null,
            IReadOnlyList<TypeValidationDiagnosticRelatedLocation>? relatedLocations = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Path = path;
            JsonPointer = jsonPointer;
            Line = line;
            Column = column;
            RelatedLocations = relatedLocations ?? NoRelatedLocations;
        }

        /// <summary>Stable diagnostic code, for example <c>BCPVT001</c>.</summary>
        public string Code { get; }

        /// <summary>Severity of the diagnostic.</summary>
        public TypeValidationDiagnosticSeverity Severity { get; }

        /// <summary>Human-readable diagnostic message.</summary>
        public string Message { get; }

        /// <summary>Package-relative path of the diagnostic, when available.</summary>
        public string? Path { get; }

        /// <summary>JSON pointer into the offending file, when available.</summary>
        public string? JsonPointer { get; }

        /// <summary>1-based line number, when available.</summary>
        public int? Line { get; }

        /// <summary>1-based column number, when available.</summary>
        public int? Column { get; }

        /// <summary>Secondary locations associated with the diagnostic.</summary>
        public IReadOnlyList<TypeValidationDiagnosticRelatedLocation> RelatedLocations { get; }
    }
}
