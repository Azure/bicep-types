// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;

namespace Azure.Bicep.Types.Serialization;

[JsonSerializable(typeof(TypeBase[]))]
[JsonSerializable(typeof(TypeIndex))]
[JsonSerializable(typeof(ArrayType))]
[JsonSerializable(typeof(BuiltInType))]
[JsonSerializable(typeof(DiscriminatedObjectType))]
[JsonSerializable(typeof(ObjectType))]
[JsonSerializable(typeof(ResourceFunctionType))]
[JsonSerializable(typeof(ResourceType))]
[JsonSerializable(typeof(StringLiteralType))]
[JsonSerializable(typeof(UnionType))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class TypeJsonContext : JsonSerializerContext
{
    public static JsonSerializerOptions GetSerializerOptions(TypeFactory factory)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TypeBaseConverter(factory));
        options.AddContext<TypeJsonContext>();

        return options;
    }
}