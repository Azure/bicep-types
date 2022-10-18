// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    public class DiscriminatedObjectType : TypeBase
    {
        [JsonConstructor]
        public DiscriminatedObjectType(string name, string discriminator, IReadOnlyDictionary<string, ObjectTypeProperty> baseProperties, IReadOnlyDictionary<string, ITypeReference> elements)
            => (Name, Discriminator, BaseProperties, Elements) = (name, discriminator, baseProperties, elements);

        public string Name { get; }

        public string Discriminator { get; }

        public IReadOnlyDictionary<string, ObjectTypeProperty> BaseProperties { get; }

        public IReadOnlyDictionary<string, ITypeReference> Elements { get; }
    }
}