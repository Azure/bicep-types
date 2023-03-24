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
        public ObjectType(string name, IReadOnlyDictionary<string, ObjectTypeProperty> properties, ITypeReference? additionalProperties)
            => (Name, Properties, AdditionalProperties) = (name, properties, additionalProperties);

        public string Name { get; }

        public IReadOnlyDictionary<string, ObjectTypeProperty> Properties { get; }

        public ITypeReference? AdditionalProperties { get; }
    }

    [Flags]
    public enum ObjectTypePropertyFlags
    {
        None = 0,

        Required = 1 << 0,

        ReadOnly = 1 << 1,

        WriteOnly = 1 << 2,

        DeployTimeConstant = 1 << 3,

        Identifier = 1 << 4,
    }

    public class ObjectTypeProperty
    {

        [JsonConstructor]
        public ObjectTypeProperty(ITypeReference type, ObjectTypePropertyFlags flags, string? description, bool? secure = null)
            => (Type, Flags, Description, Secure) = (type, flags, description, secure);

        public ITypeReference Type { get; }

        public ObjectTypePropertyFlags Flags { get; }

        public string? Description { get; }

        public bool? Secure { get; }
    }
}
