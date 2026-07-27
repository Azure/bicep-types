// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Diagnostic-free parser for <c>$ref</c> string values.
    /// Splits a reference string into an optional package-relative path and a non-negative
    /// integer index.  The expected formats are <c>#/3</c> (same-file) and
    /// <c>types.json#/0</c> (cross-file).
    /// <para>
    /// This type intentionally produces no diagnostics so that <see cref="PackageReader"/>
    /// can discover which type files to load without depending on structural validators.
    /// <c>ReferenceSyntax</c> in the Structural layer adds diagnostic production on top of
    /// the same parsing logic.
    /// </para>
    /// </summary>
    internal static class ReferencePath
    {
        private const string FragmentSeparator = "#/";

        /// <summary>
        /// Attempts to parse a <c>$ref</c> string value.
        /// Returns <c>true</c> on success and sets <paramref name="packageRelativePath"/>
        /// (empty string for same-file refs) and <paramref name="index"/>.
        /// </summary>
        public static bool TryParse(string refValue, out string packageRelativePath, out int index)
        {
            if (string.IsNullOrEmpty(refValue))
            {
                packageRelativePath = string.Empty;
                index = -1;
                return false;
            }

            int sepIndex = refValue.IndexOf(FragmentSeparator, StringComparison.Ordinal);
            if (sepIndex < 0)
            {
                packageRelativePath = string.Empty;
                index = -1;
                return false;
            }

            string indexText = refValue.Substring(sepIndex + FragmentSeparator.Length);

            if (!int.TryParse(indexText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int parsedIndex) || parsedIndex < 0)
            {
                packageRelativePath = string.Empty;
                index = -1;
                return false;
            }

            packageRelativePath = refValue.Substring(0, sepIndex); // empty = same-file ref
            index = parsedIndex;
            return true;
        }

        /// <summary>
        /// Returns the package-relative path portion of a <c>$ref</c> string, or
        /// <c>null</c> if the string is not a valid cross-file reference.
        /// Returns an empty string for valid same-file references.
        /// </summary>
        public static string? ExtractPackagePath(string refValue)
        {
            return TryParse(refValue, out string path, out _) ? path : null;
        }
    }
}
