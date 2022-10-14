// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;

namespace Azure.Bicep.Types.Serialization
{
    public static class TypeSerializer
    {
        public static void Serialize(Stream stream, TypeBase[] types)
        {
            var factory = new TypeFactory(types);

            var serializeOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            serializeOptions.Converters.Add(new TypeBaseConverter(factory));

            JsonSerializer.Serialize(stream, types, serializeOptions);
        }

        public static TypeBase[] Deserialize(Stream contentStream)
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());

            var serializeOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            serializeOptions.Converters.Add(new TypeBaseConverter(factory));

            var types = JsonSerializer.Deserialize<TypeBase[]>(contentStream, serializeOptions) ?? throw new JsonException("Failed to deserialize content");

            factory.Hydrate(types);

            return types;
        }

        public static TypeIndex DeserializeIndex(Stream contentStream)
        {
            var serializeOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            return JsonSerializer.Deserialize<TypeIndex>(contentStream, serializeOptions) ?? throw new JsonException("Failed to deserialize index");
        }
    }
}
