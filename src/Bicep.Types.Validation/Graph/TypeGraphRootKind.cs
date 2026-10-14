// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// Kind of a graph root reference discovered from <c>index.json</c>.
    /// </summary>
    internal enum TypeGraphRootKind
    {
        Resource,
        ResourceFunction,
        NamespaceFunction,
        ConfigurationType,
        FallbackResourceType,
    }
}
