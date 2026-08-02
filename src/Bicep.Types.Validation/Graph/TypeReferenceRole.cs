// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// The role a reference plays in the package graph.  The role determines which
    /// target type-object kinds are allowed and whether a mismatch is reported as a
    /// top-level or nested diagnostic.
    /// </summary>
    internal enum TypeReferenceRole
    {
        // Top-level roots from index.json
        ResourceRoot,
        ResourceFunctionRoot,
        NamespaceFunctionRoot,
        FallbackResourceType,
        ConfigurationType,

        // Nested edges from type objects
        ResourceBody,
        ObjectPropertyType,
        AdditionalProperties,
        ArrayItem,
        UnionMember,
        FunctionParameter,
        FunctionOutput,
        ResourceFunctionInput,
        ResourceFunctionOutput,
        NamespaceFunctionParameter,
        NamespaceFunctionOutput,
        ResourceTypeFunction,
        DiscriminatedObjectElement,
    }
}
