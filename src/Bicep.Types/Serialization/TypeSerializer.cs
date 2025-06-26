// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;

namespace Azure.Bicep.Types.Serialization;

public static class TypeSerializer
{
    public static JsonSerializerOptions GetSerializerOptions(TypeFactory? factory = null)
    {
        factory ??= new(Enumerable.Empty<TypeBase>());

        return new JsonSerializerOptions
        {
            Converters = {
                new TypeReferenceConverter(factory),
                new CrossFileTypeReferenceConverter(),
            },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            TypeInfoResolver = TypeJsonContext.Default,
        };
    }

    public static void Serialize(Stream stream, TypeBase[] types)
    {
        var options = GetSerializerOptions(new(types));

        JsonSerializer.Serialize(stream, types, options);
    }

    public static TypeBase[] Deserialize(Stream contentStream)
    {
        var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
        var options = GetSerializerOptions(factory);
        // 'jsonTypeInfo' will have the original 'options' attached along with the type info for TypeBase[].
        var jsonTypeInfo = (JsonTypeInfo<TypeBase[]>) options.GetTypeInfo(typeof(TypeBase[]));

        var types = JsonSerializer.Deserialize(contentStream, jsonTypeInfo) ?? throw new JsonException("Failed to deserialize content");

        factory.Hydrate(types);

        return types;
    }

    public static TypeIndex DeserializeIndex(Stream contentStream)
    {
        var options = GetSerializerOptions();
        var jsonTypeInfo = (JsonTypeInfo<TypeIndex>) options.GetTypeInfo(typeof(TypeIndex));

        return JsonSerializer.Deserialize(contentStream, jsonTypeInfo) ?? throw new JsonException("Failed to deserialize index");
    }

    public static void SerializeIndex(Stream stream, TypeIndex index)
    {
        var options = GetSerializerOptions();

        JsonSerializer.Serialize(stream, index, options);
    }
}