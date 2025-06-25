// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    [SuppressMessage("Trimming", 
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", 
        Justification = "TypeBase[] is included in TypeJsonContext via [JsonSerializable(typeof(TypeBase[]))], providing required type metadata for trimming-safe serialization.")]
    public static TypeBase[] Deserialize(Stream contentStream)
    {
        var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
        var options = GetSerializerOptions(factory);

        var types = JsonSerializer.Deserialize<TypeBase[]>(contentStream, options)
            ?? throw new JsonException("Failed to deserialize content");

        factory.Hydrate(types);

        return types;
    }

    [SuppressMessage("Trimming", 
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", 
        Justification = "TypeIndex is included in TypeJsonContext via [JsonSerializable(typeof(TypeIndex))], providing required type metadata for trimming-safe serialization.")]
    public static TypeIndex DeserializeIndex(Stream contentStream)
    {
        var options = GetSerializerOptions();

        return JsonSerializer.Deserialize<TypeIndex>(contentStream, options)
            ?? throw new JsonException("Failed to deserialize index");
    }

    public static void SerializeIndex(Stream stream, TypeIndex index)
    {
        var options = GetSerializerOptions();

        JsonSerializer.Serialize(stream, index, options);
    }
}