// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// One outgoing reference from a type object field.
    /// </summary>
    internal sealed class TypeGraphEdge
    {
        public TypeGraphEdge(
            TypeNodeId sourceNodeId,
            TypeReferenceRole role,
            ParsedTypeReference reference,
            string? memberName = null)
        {
            SourceNodeId = sourceNodeId;
            Role = role;
            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
            MemberName = memberName;
        }

        /// <summary>Identity of the type object that owns this reference.</summary>
        public TypeNodeId SourceNodeId { get; }

        /// <summary>The role used to validate the target kind.</summary>
        public TypeReferenceRole Role { get; }

        /// <summary>The parsed reference.</summary>
        public ParsedTypeReference Reference { get; }

        /// <summary>Optional member name (property, variant, or parameter) for context.</summary>
        public string? MemberName { get; }
    }
}
