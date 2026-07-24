// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// Encodes which target type-object kinds are valid for each reference role, and provides
    /// human-readable expectation text for diagnostics.
    /// </summary>
    internal static class TypeTargetKindValidator
    {
        // Kinds usable as a "type value" (a property type, array item, union member, function
        // parameter/output, etc.). Excludes the resource/function container kinds.
        private static readonly HashSet<string> ValueTypeKinds = new HashSet<string>(System.StringComparer.Ordinal)
        {
            "AnyType", "NullType", "BooleanType", "IntegerType", "StringType", "StringLiteralType",
            "ObjectType", "DiscriminatedObjectType", "ArrayType", "UnionType", "BuiltInType",
        };

        private static readonly HashSet<string> ObjectLikeKinds = new HashSet<string>(System.StringComparer.Ordinal)
        {
            "ObjectType", "DiscriminatedObjectType",
        };

        /// <summary>Returns <c>true</c> if roots of this role are validated as top-level (<c>index.json</c>) references.</summary>
        public static bool IsTopLevel(TypeReferenceRole role)
        {
            switch (role)
            {
                case TypeReferenceRole.ResourceRoot:
                case TypeReferenceRole.ResourceFunctionRoot:
                case TypeReferenceRole.NamespaceFunctionRoot:
                case TypeReferenceRole.FallbackResourceType:
                case TypeReferenceRole.ConfigurationType:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Returns <c>true</c> if <paramref name="discriminator"/> is an allowed target for <paramref name="role"/>.</summary>
        public static bool IsAllowed(TypeReferenceRole role, string discriminator)
        {
            switch (role)
            {
                case TypeReferenceRole.ResourceRoot:
                case TypeReferenceRole.FallbackResourceType:
                    return discriminator == "ResourceType";

                case TypeReferenceRole.ResourceFunctionRoot:
                    return discriminator == "ResourceFunctionType";

                case TypeReferenceRole.NamespaceFunctionRoot:
                    return discriminator == "NamespaceFunctionType";

                case TypeReferenceRole.ConfigurationType:
                case TypeReferenceRole.ResourceBody:
                    return ObjectLikeKinds.Contains(discriminator);

                case TypeReferenceRole.ResourceTypeFunction:
                    return discriminator == "FunctionType";

                case TypeReferenceRole.DiscriminatedObjectElement:
                    return discriminator == "ObjectType";

                default:
                    // All remaining roles are value-type positions.
                    return ValueTypeKinds.Contains(discriminator);
            }
        }

        /// <summary>Returns a human-readable description of the allowed target kinds for a role.</summary>
        public static string ExpectedText(TypeReferenceRole role)
        {
            switch (role)
            {
                case TypeReferenceRole.ResourceRoot:
                case TypeReferenceRole.FallbackResourceType:
                    return "a resource type ('ResourceType')";

                case TypeReferenceRole.ResourceFunctionRoot:
                    return "a resource function type ('ResourceFunctionType')";

                case TypeReferenceRole.NamespaceFunctionRoot:
                    return "a namespace function type ('NamespaceFunctionType')";

                case TypeReferenceRole.ConfigurationType:
                case TypeReferenceRole.ResourceBody:
                    return "an object type ('ObjectType' or 'DiscriminatedObjectType')";

                case TypeReferenceRole.ResourceTypeFunction:
                    return "a function type ('FunctionType')";

                case TypeReferenceRole.DiscriminatedObjectElement:
                    return "an object type ('ObjectType')";

                default:
                    return "a value type";
            }
        }
    }
}
