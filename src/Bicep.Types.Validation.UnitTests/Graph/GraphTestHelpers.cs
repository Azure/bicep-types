// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.UnitTests.Graph;

/// <summary>
/// Shared helpers for graph-layer tests: parsing JSON into <see cref="PackageDocument"/>s and
/// an in-memory <see cref="IPackageFileSystem"/> test double.
/// </summary>
internal static class GraphTestHelpers
{
    public static PackageDocument Document(string packageRelativePath, string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        SourceMap.TryParse(bytes, packageRelativePath, out var root, out var sourceMap, out _);
        var kind = packageRelativePath == "index.json"
            ? PackageDocumentKind.Index
            : PackageDocumentKind.TypeFile;
        return new PackageDocument(packageRelativePath, kind, root!, sourceMap);
    }
}

/// <summary>
/// An in-memory <see cref="IPackageFileSystem"/>. A file mapped to <c>null</c> is treated as
/// existing but unreadable (to exercise read-failure paths).
/// </summary>
internal sealed class InMemoryPackageFileSystem : IPackageFileSystem
{
    private readonly Dictionary<string, byte[]?> files =
        new Dictionary<string, byte[]?>(StringComparer.OrdinalIgnoreCase);

    public InMemoryPackageFileSystem AddText(string packageRelativePath, string content)
    {
        files[packageRelativePath] = Encoding.UTF8.GetBytes(content);
        return this;
    }

    public InMemoryPackageFileSystem AddUnreadable(string packageRelativePath)
    {
        files[packageRelativePath] = null;
        return this;
    }

    public bool FileExists(string packageRelativePath) => files.ContainsKey(packageRelativePath);

    public bool TryReadAllBytes(string packageRelativePath, out byte[] bytes, out string error)
    {
        if (files.TryGetValue(packageRelativePath, out var content) && content != null)
        {
            bytes = content;
            error = string.Empty;
            return true;
        }

        bytes = Array.Empty<byte>();
        error = "simulated read failure";
        return false;
    }

    public IEnumerable<string> EnumerateFiles() => files.Keys;
}
