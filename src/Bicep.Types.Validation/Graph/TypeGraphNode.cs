// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// One structurally usable type object in the package graph.
    /// </summary>
    internal sealed class TypeGraphNode
    {
        public TypeGraphNode(
            TypeNodeId id,
            string discriminator,
            JsonValueNode objectNode,
            PackageDocument document,
            string jsonPointer,
            SourceLocation location)
        {
            Id = id;
            Discriminator = discriminator ?? throw new ArgumentNullException(nameof(discriminator));
            ObjectNode = objectNode ?? throw new ArgumentNullException(nameof(objectNode));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            JsonPointer = jsonPointer ?? throw new ArgumentNullException(nameof(jsonPointer));
            Location = location;
        }

        /// <summary>Stable identity of this type object.</summary>
        public TypeNodeId Id { get; }

        /// <summary>The <c>$type</c> discriminator, e.g. <c>ObjectType</c>.</summary>
        public string Discriminator { get; }

        /// <summary>The JSON object node backing this type object.</summary>
        public JsonValueNode ObjectNode { get; }

        /// <summary>The document that contains this type object.</summary>
        public PackageDocument Document { get; }

        /// <summary>JSON pointer of this type object within its file (e.g. <c>/0</c>).</summary>
        public string JsonPointer { get; }

        /// <summary>Source location of this type object.</summary>
        public SourceLocation Location { get; }
    }
}
