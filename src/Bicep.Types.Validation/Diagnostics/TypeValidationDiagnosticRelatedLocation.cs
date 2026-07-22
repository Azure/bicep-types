// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation.Diagnostics
{
    /// <summary>
    /// A secondary source location associated with a diagnostic, such as the
    /// declaration site referenced by a wrong-target-kind diagnostic.
    /// </summary>
    public sealed class TypeValidationDiagnosticRelatedLocation
    {
        public TypeValidationDiagnosticRelatedLocation(
            string message,
            string? path = null,
            string? jsonPointer = null,
            int? line = null,
            int? column = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Path = path;
            JsonPointer = jsonPointer;
            Line = line;
            Column = column;
        }

        /// <summary>Human-readable note describing the related location.</summary>
        public string Message { get; }

        /// <summary>Package-relative path of the related location, when available.</summary>
        public string? Path { get; }

        /// <summary>JSON pointer into the related file, when available.</summary>
        public string? JsonPointer { get; }

        /// <summary>1-based line number, when available.</summary>
        public int? Line { get; }

        /// <summary>1-based column number, when available.</summary>
        public int? Column { get; }
    }
}
