// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Internal abstraction over package file access.  Implementations must normalize
    /// path separators to <c>/</c> and reject paths that escape the package root.
    /// </summary>
    internal interface IPackageFileSystem
    {
        /// <summary>Returns <c>true</c> if the package-relative path exists.</summary>
        bool FileExists(string packageRelativePath);

        /// <summary>
        /// Reads the package-relative file as UTF-8 bytes.
        /// Returns <c>false</c> and sets <paramref name="error"/> on failure.
        /// </summary>
        bool TryReadAllBytes(string packageRelativePath, out byte[] bytes, out string error);

        /// <summary>
        /// Enumerates known package-relative file paths for unreachable-file checking.
        /// May return an empty enumerable if enumeration is not supported.
        /// </summary>
        IEnumerable<string> EnumerateFiles();
    }
}
