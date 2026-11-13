// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Diagnostics
{
    /// <summary>
    /// Central deterministic ordering for validation diagnostics.
    /// </summary>
    /// <remarks>
    /// Sort order:
    /// <list type="number">
    /// <item>Input-level diagnostics with no package path sort before file-scoped diagnostics.</item>
    /// <item>Package-relative path.</item>
    /// <item>Line.</item>
    /// <item>Column.</item>
    /// <item>Diagnostic code.</item>
    /// <item>JSON pointer.</item>
    /// <item>Message as the final tie-breaker.</item>
    /// </list>
    /// </remarks>
    public sealed class TypeValidationDiagnosticComparer : IComparer<TypeValidationDiagnostic>
    {
        /// <summary>Shared stateless instance.</summary>
        public static TypeValidationDiagnosticComparer Instance { get; } = new TypeValidationDiagnosticComparer();

        public int Compare(TypeValidationDiagnostic? x, TypeValidationDiagnostic? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            // 1. Location scope: input-level (no package path) sorts before file-scoped.
            var xHasPath = !string.IsNullOrEmpty(x.Path);
            var yHasPath = !string.IsNullOrEmpty(y.Path);
            if (xHasPath != yHasPath)
            {
                return xHasPath ? 1 : -1;
            }

            // 2. Package-relative path.
            var cmp = string.CompareOrdinal(x.Path ?? string.Empty, y.Path ?? string.Empty);
            if (cmp != 0)
            {
                return cmp;
            }

            // 3. Line.
            cmp = CompareNullable(x.Line, y.Line);
            if (cmp != 0)
            {
                return cmp;
            }

            // 4. Column.
            cmp = CompareNullable(x.Column, y.Column);
            if (cmp != 0)
            {
                return cmp;
            }

            // 5. Diagnostic code.
            cmp = string.CompareOrdinal(x.Code, y.Code);
            if (cmp != 0)
            {
                return cmp;
            }

            // 6. JSON pointer.
            cmp = string.CompareOrdinal(x.JsonPointer ?? string.Empty, y.JsonPointer ?? string.Empty);
            if (cmp != 0)
            {
                return cmp;
            }

            // 7. Message.
            return string.CompareOrdinal(x.Message, y.Message);
        }

        private static int CompareNullable(int? a, int? b)
        {
            if (!a.HasValue && !b.HasValue)
            {
                return 0;
            }

            // A missing line/column sorts before a present one.
            if (!a.HasValue)
            {
                return -1;
            }

            if (!b.HasValue)
            {
                return 1;
            }

            return a.Value.CompareTo(b.Value);
        }
    }
}
