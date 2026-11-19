// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// One root reference discovered from <c>index.json</c>.
    /// </summary>
    internal sealed class TypeGraphRoot
    {
        public TypeGraphRoot(
            TypeGraphRootKind kind,
            string description,
            TypeReferenceRole role,
            ParsedTypeReference reference)
        {
            Kind = kind;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Role = role;
            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        }

        /// <summary>The kind of root.</summary>
        public TypeGraphRootKind Kind { get; }

        /// <summary>Human-readable description used in diagnostics, e.g. <c>Resource entry 'Foo/bar@v1'</c>.</summary>
        public string Description { get; }

        /// <summary>The role used to validate the target kind.</summary>
        public TypeReferenceRole Role { get; }

        /// <summary>The parsed reference to the root type object.</summary>
        public ParsedTypeReference Reference { get; }
    }
}
