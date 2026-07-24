// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Policy;
using Azure.Bicep.Types.Validation.UnitTests.Graph;

namespace Azure.Bicep.Types.Validation.UnitTests.Policy;

/// <summary>
/// Shared helpers for policy-layer tests. Mirrors the driver: the graph traversal populates the
/// provider cache with reached type files, then the mode-policy layer classifies them.
/// </summary>
internal static class PolicyTestHelpers
{
    public static IReadOnlyList<TypeValidationDiagnostic> RunPolicy(
        string indexJson, InMemoryPackageFileSystem fs, TypePackageValidationMode mode)
    {
        var options = new TypePackageValidationOptions { Mode = mode };
        var index = GraphTestHelpers.Document("index.json", indexJson);
        var provider = new PackageDocumentProvider(fs, index, options);

        // Trigger graph traversal so the provider loads and caches the reached type files.
        SemanticGraphValidator.Validate(provider, index, options);

        return PolicyValidator.Validate(provider.GetReachedUsableTypeFiles(), options);
    }

    /// <summary>An index that routes a single resource type to <paramref name="refValue"/>.</summary>
    public static string ResourceIndex(string refValue) =>
        "{\"resources\":{\"My.Rp/x@2026-01-01\":{\"$ref\":\"" + refValue + "\"}}," +
        "\"resourceFunctions\":{},\"namespaceFunctions\":[]}";
}
