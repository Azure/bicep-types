// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Directory-backed implementation of <see cref="IPackageFileSystem"/>.
    /// Package-relative paths are resolved against a physical root directory.
    /// Absolute paths and <c>..</c> traversal outside the root are rejected.
    /// </summary>
    internal sealed class DirectoryPackageFileSystem : IPackageFileSystem
    {
        private static readonly StringComparison PhysicalPathComparison =
            Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        private readonly string physicalRoot;

        public DirectoryPackageFileSystem(string physicalRoot)
        {
            this.physicalRoot = physicalRoot ?? throw new ArgumentNullException(nameof(physicalRoot));
        }

        /// <inheritdoc/>
        public bool FileExists(string packageRelativePath)
        {
            if (!TryResolvePhysicalPath(packageRelativePath, out string? physical))
            {
                return false;
            }
            return File.Exists(physical);
        }

        /// <inheritdoc/>
        public bool TryReadAllBytes(string packageRelativePath, out byte[] bytes, out string error)
        {
            if (!TryResolvePhysicalPath(packageRelativePath, out string? physical))
            {
                bytes = Array.Empty<byte>();
                error = $"Package-relative path '{packageRelativePath}' is not valid or escapes the package root.";
                return false;
            }

            try
            {
                bytes = File.ReadAllBytes(physical!);
                error = string.Empty;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                bytes = Array.Empty<byte>();
                error = ex.Message;
                return false;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> EnumerateFiles()
        {
            if (!Directory.Exists(physicalRoot))
            {
                return Enumerable.Empty<string>();
            }

            var rootFull = NormalizeRoot(physicalRoot);
            return Directory.EnumerateFiles(physicalRoot, "*", SearchOption.AllDirectories)
                .Select(f =>
                {
                    var rel = Path.GetFullPath(f).Substring(rootFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    return rel.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
                });
        }

        /// <summary>
        /// Resolves a package-relative path to a physical path under the root.
        /// Returns <c>false</c> if the path is absolute, empty, or would escape the root.
        /// Diagnostics use the package-relative path; the physical path is returned only for file IO.
        /// </summary>
        private bool TryResolvePhysicalPath(string packageRelativePath, out string? physical)
        {
            if (string.IsNullOrEmpty(packageRelativePath))
            {
                physical = null;
                return false;
            }

            packageRelativePath = NormalizeSeparators(packageRelativePath);

            // Reject absolute paths
            if (IsPackagePathRooted(packageRelativePath))
            {
                physical = null;
                return false;
            }

            // Combine and normalize
            string combined;
            try
            {
                combined = Path.GetFullPath(Path.Combine(physicalRoot, packageRelativePath));
            }
            catch
            {
                physical = null;
                return false;
            }

            // Reject traversal outside the root. The normalized root has any trailing
            // separator removed so a directory root supplied with a trailing slash (e.g.
            // "C:\\pkg\\") still matches its own children (e.g. "C:\\pkg\\index.json").
            string rootFull = NormalizeRoot(physicalRoot);
            if (!combined.StartsWith(rootFull + Path.DirectorySeparatorChar, PhysicalPathComparison) &&
                !combined.StartsWith(rootFull + Path.AltDirectorySeparatorChar, PhysicalPathComparison) &&
                !string.Equals(combined, rootFull, PhysicalPathComparison))
            {
                physical = null;
                return false;
            }

            physical = combined;
            return true;
        }

        /// <summary>
        /// Returns the absolute root path with any trailing directory separators removed,
        /// so prefix-based containment checks are not defeated by a trailing slash.
        /// </summary>
        private static string NormalizeRoot(string root) =>
            Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        private static bool IsPackagePathRooted(string path) =>
            Path.IsPathRooted(path) ||
            (path.Length >= 2 && path[1] == ':' && IsAsciiLetter(path[0]));

        private static bool IsAsciiLetter(char value) =>
            (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');

        /// <summary>Normalizes a package-relative path to use forward slashes.</summary>
        public static string NormalizeSeparators(string path) =>
            path.Replace('\\', '/');
    }
}
