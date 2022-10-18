// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.IO;
using System.Linq;
using System.Text.Json;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;

namespace Azure.Bicep.Types.Serialization
{
    public static class TypeSerializer
    {
        public static void Serialize(Stream stream, TypeBase[] types)
        {
            var factory = new TypeFactory(types);

            JsonSerializer.Serialize(stream, types, TypeJsonContext.GetSerializerOptions(factory));
        }

        public static TypeBase[] Deserialize(Stream contentStream)
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());

            var types = JsonSerializer.Deserialize<TypeBase[]>(contentStream, TypeJsonContext.GetSerializerOptions(factory))
                ?? throw new JsonException("Failed to deserialize content");

            factory.Hydrate(types);

            return types;
        }

        public static TypeIndex DeserializeIndex(Stream contentStream)
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());

            return JsonSerializer.Deserialize<TypeIndex>(contentStream, TypeJsonContext.GetSerializerOptions(factory))
                ?? throw new JsonException("Failed to deserialize index");
        }
    }
}
