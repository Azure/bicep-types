// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Normalized classification of a validation input.
    /// </summary>
    internal enum PackageInputKind
    {
        Directory,
        IndexFile,
        ArchiveFile,
        ArchiveStream,
    }
}
