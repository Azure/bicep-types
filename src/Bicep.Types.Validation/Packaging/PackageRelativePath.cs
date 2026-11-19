// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>Reason a package-relative path could not be canonicalized.</summary>
    internal enum PackageRelativePathError
    {
        /// <summary>The path is valid.</summary>
        None,

        /// <summary>The path does not identify a package file.</summary>
        Empty,

        /// <summary>The path begins at a filesystem root.</summary>
        Rooted,

        /// <summary>The first retained segment uses drive-qualified or drive-relative syntax.</summary>
        DriveQualified,

        /// <summary>The path contains an exact parent-directory segment.</summary>
        ParentTraversal,

        /// <summary>The path ends with a directory separator.</summary>
        TrailingSeparator,
    }

    /// <summary>
    /// Produces the OS-independent lexical identity used for files within a type package.
    /// </summary>
    internal static class PackageRelativePath
    {
        /// <summary>Canonicalizes a nonempty package file path.</summary>
        public static bool TryCanonicalizeFile(
            string path,
            out string canonicalPath,
            out PackageRelativePathError error) =>
            TryCanonicalize(path, allowOriginallyEmpty: false, out canonicalPath, out error);

        /// <summary>Canonicalizes a reference target, allowing an originally empty same-file path.</summary>
        public static bool TryCanonicalizeReferenceTarget(
            string path,
            out string canonicalPath,
            out PackageRelativePathError error) =>
            TryCanonicalize(path, allowOriginallyEmpty: true, out canonicalPath, out error);

        /// <summary>Applies the shared lexical package path rules.</summary>
        private static bool TryCanonicalize(
            string path,
            bool allowOriginallyEmpty,
            out string canonicalPath,
            out PackageRelativePathError error)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            canonicalPath = string.Empty;
            error = PackageRelativePathError.None;

            if (path.Length == 0)
            {
                if (allowOriginallyEmpty)
                {
                    return true;
                }

                error = PackageRelativePathError.Empty;
                return false;
            }

            string normalized = path.Replace('\\', '/');
            if (normalized[0] == '/')
            {
                error = PackageRelativePathError.Rooted;
                return false;
            }

            if (normalized.EndsWith("/", StringComparison.Ordinal))
            {
                error = PackageRelativePathError.TrailingSeparator;
                return false;
            }

            var canonicalSegments = new List<string>();
            foreach (string segment in normalized.Split('/'))
            {
                if (segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(segment, "..", StringComparison.Ordinal))
                {
                    error = PackageRelativePathError.ParentTraversal;
                    return false;
                }

                if (canonicalSegments.Count == 0 && segment.Length >= 2 && segment[1] == ':')
                {
                    error = PackageRelativePathError.DriveQualified;
                    return false;
                }

                canonicalSegments.Add(segment);
            }

            if (canonicalSegments.Count == 0)
            {
                error = PackageRelativePathError.Empty;
                return false;
            }

            canonicalPath = string.Join("/", canonicalSegments);
            return true;
        }
    }
}