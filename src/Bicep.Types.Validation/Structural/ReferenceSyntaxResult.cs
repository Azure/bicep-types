// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Parsed result from validating a single reference object.
    /// Carries the package-relative path and integer index when valid.
    /// </summary>
    internal sealed class ReferenceSyntaxResult
    {
        private ReferenceSyntaxResult(bool isValid, string packageRelativePath, int index)
        {
            IsValid = isValid;
            PackageRelativePath = packageRelativePath;
            Index = index;
        }

        /// <summary><c>true</c> when the reference object is structurally valid.</summary>
        public bool IsValid { get; }

        /// <summary>
        /// Package-relative path of the referenced file.
        /// Empty string for same-file references.
        /// Only meaningful when <see cref="IsValid"/> is <c>true</c>.
        /// </summary>
        public string PackageRelativePath { get; }

        /// <summary>
        /// 0-based integer index within the referenced file.
        /// Only meaningful when <see cref="IsValid"/> is <c>true</c>.
        /// </summary>
        public int Index { get; }

        internal static ReferenceSyntaxResult Valid(string packageRelativePath, int index) =>
            new ReferenceSyntaxResult(true, packageRelativePath, index);

        internal static readonly ReferenceSyntaxResult Invalid =
            new ReferenceSyntaxResult(false, string.Empty, -1);
    }
}
