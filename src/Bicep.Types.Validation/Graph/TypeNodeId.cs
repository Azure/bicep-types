// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// Stable identity for a type object: its package-relative file path and its index
    /// within that type file's array.  Paths are compared case-insensitively to match the
    /// package file-system and <c>JsonDocumentSet</c> behavior, so references that differ
    /// only by path case identify the same node.
    /// </summary>
    internal readonly struct TypeNodeId : IEquatable<TypeNodeId>
    {
        public TypeNodeId(string packageRelativePath, int index)
        {
            PackageRelativePath = packageRelativePath ?? throw new ArgumentNullException(nameof(packageRelativePath));
            Index = index;
        }

        /// <summary>Package-relative path of the type file (forward-slash normalized).</summary>
        public string PackageRelativePath { get; }

        /// <summary>0-based index of the type object within the type file's array.</summary>
        public int Index { get; }

        public bool Equals(TypeNodeId other) =>
            Index == other.Index &&
            string.Equals(PackageRelativePath, other.PackageRelativePath, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => obj is TypeNodeId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(PackageRelativePath);
                hash = (hash * 31) + Index;
                return hash;
            }
        }

        public override string ToString() => $"{PackageRelativePath}#/{Index}";
    }
}
