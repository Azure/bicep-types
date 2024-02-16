// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

[JsonDerivedType(typeof(ArrayType), typeDiscriminator: nameof(ArrayType))]
[JsonDerivedType(typeof(BuiltInType), typeDiscriminator: nameof(BuiltInType))]
[JsonDerivedType(typeof(DiscriminatedObjectType), typeDiscriminator: nameof(DiscriminatedObjectType))]
[JsonDerivedType(typeof(ObjectType), typeDiscriminator: nameof(ObjectType))]
[JsonDerivedType(typeof(ResourceFunctionType), typeDiscriminator: nameof(ResourceFunctionType))]
[JsonDerivedType(typeof(ResourceType), typeDiscriminator: nameof(ResourceType))]
[JsonDerivedType(typeof(StringLiteralType), typeDiscriminator: nameof(StringLiteralType))]
[JsonDerivedType(typeof(UnionType), typeDiscriminator: nameof(UnionType))]
[JsonDerivedType(typeof(AnyType), typeDiscriminator: nameof(AnyType))]
[JsonDerivedType(typeof(NullType), typeDiscriminator: nameof(NullType))]
[JsonDerivedType(typeof(BooleanType), typeDiscriminator: nameof(BooleanType))]
[JsonDerivedType(typeof(IntegerType), typeDiscriminator: nameof(IntegerType))]
[JsonDerivedType(typeof(StringType), typeDiscriminator: nameof(StringType))]
public abstract class TypeBase
{
}