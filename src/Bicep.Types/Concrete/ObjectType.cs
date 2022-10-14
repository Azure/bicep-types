// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    public class ObjectType : TypeBase
    {
        [JsonConstructor]
        public ObjectType(string name, IReadOnlyDictionary<string, ObjectProperty> properties, ITypeReference? additionalProperties)
            => (Name, Properties, AdditionalProperties) = (name, properties, additionalProperties);

        public string Name { get; }

        public IReadOnlyDictionary<string, ObjectProperty> Properties { get; }

        public ITypeReference? AdditionalProperties { get; }
    }

    [Flags]
    public enum ObjectPropertyFlags
    {
        None = 0,

        Required = 1 << 0,

        ReadOnly = 1 << 1,

        WriteOnly = 1 << 2,

        DeployTimeConstant = 1 << 3,
    }

    public class ObjectProperty
    {

        [JsonConstructor]
        public ObjectProperty(ITypeReference type, ObjectPropertyFlags flags, string? description)
            => (Type, Flags, Description) = (type, flags, description);

        public ITypeReference Type { get; }

        public ObjectPropertyFlags Flags { get; }

        public string? Description { get; }
    }
}