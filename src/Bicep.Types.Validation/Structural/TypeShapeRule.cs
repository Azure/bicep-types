// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Expected JSON value shape for a type-object field.
    /// </summary>
    internal enum FieldShape
    {
        /// <summary>No field shape has been specified.</summary>
        Unspecified,

        /// <summary>JSON string value.</summary>
        String,

        /// <summary>JSON boolean value.</summary>
        Bool,

        /// <summary>JSON number value (integer).</summary>
        Integer,

        /// <summary>A reference object: <c>{"$ref": "..."}</c>.</summary>
        Ref,

        /// <summary>JSON array where each element is a reference object.</summary>
        ArrayOfRefs,

        /// <summary>JSON object whose values are reference objects.</summary>
        ObjectMapOfReferences,

        /// <summary>JSON object whose values are objects with a nested <c>type</c> reference.</summary>
        ObjectMapOfObjectsWithTypeReference,

        /// <summary>JSON array of objects with a nested <c>type</c> reference.</summary>
        ArrayOfObjectsWithTypeReference,
    }

    /// <summary>
    /// Descriptor for one field of a type object kind.
    /// </summary>
    internal sealed class TypeFieldDescriptor
    {
        public TypeFieldDescriptor(string name, FieldShape shape, bool required, bool legacyCompatOnly = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Shape = shape;
            Required = required;
            LegacyCompatOnly = legacyCompatOnly;
        }

        /// <summary>JSON property name (camelCase).</summary>
        public string Name { get; }

        /// <summary>Expected JSON shape for this field.</summary>
        public FieldShape Shape { get; }

        /// <summary><c>true</c> if the field must be present.</summary>
        public bool Required { get; }

        /// <summary>
        /// <c>true</c> if this field is a documented legacy-compatibility field accepted by
        /// <c>CompatibleReader</c> but rejected as unknown in <c>CanonicalWriter</c>.
        /// </summary>
        public bool LegacyCompatOnly { get; }
    }
}
