// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Serialization;

internal class CrossFileTypeReferenceConverter : JsonConverter<CrossFileTypeReference>
{
    public override CrossFileTypeReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        
        reader.Read();

        if (reader.TokenType != JsonTokenType.PropertyName || 
            reader.GetString() != "$ref")
        {
            throw new JsonException();
        }

        reader.Read();

        if (reader.TokenType != JsonTokenType.String ||
            reader.GetString() is not { } stringVal)
        {
            throw new JsonException();
        }

        var pathSepIndex = stringVal.IndexOf("#/");
        if (pathSepIndex is -1)
        {
            throw new JsonException();
        }

        var relativePath = stringVal.Substring(0, pathSepIndex);
        var index = int.Parse(stringVal.Substring(pathSepIndex + 2));

        reader.Read();

        return new CrossFileTypeReference(relativePath, index);
    }

    public override void Write(Utf8JsonWriter writer, CrossFileTypeReference value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("$ref");
        writer.WriteStringValue($"{value.RelativePath}#/{value.Index}");

        writer.WriteEndObject();
    }
}
