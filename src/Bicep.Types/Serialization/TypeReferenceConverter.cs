// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace Azure.Bicep.Types.Serialization;

internal class TypeReferenceConverter : JsonConverter<ITypeReference>
{
    private const string ReferencePrefix = "#/";
    private readonly TypeFactory factory;

    public TypeReferenceConverter(TypeFactory factory)
    {
        this.factory = factory;
    }

    public override ITypeReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
            reader.GetString() is not { } stringVal ||
            !stringVal.StartsWith(ReferencePrefix, StringComparison.InvariantCulture) ||
            !int.TryParse(stringVal.Substring(ReferencePrefix.Length), out var index))
        {
            throw new JsonException();
        }

        reader.Read();

        return factory.GetReference(index);
    }

    public override void Write(Utf8JsonWriter writer, ITypeReference value, JsonSerializerOptions options)
    {
        var index = factory.GetIndex(value.Type);

        writer.WriteStartObject();

        writer.WritePropertyName("$ref");
        writer.WriteStringValue($"{ReferencePrefix}{index}");

        writer.WriteEndObject();
    }
}
