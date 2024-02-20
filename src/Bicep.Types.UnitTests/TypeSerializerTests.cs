// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.UnitTests
{
    [TestClass]
    public class TypeSerializerTests
    {
        [TestMethod]
        public void BuiltInType_can_be_serialized_and_deserialized()
        {
            var builtIns = new []
            {
                #pragma warning disable 618
                new BuiltInType(BuiltInTypeKind.Any),
                new BuiltInType(BuiltInTypeKind.Null),
                new BuiltInType(BuiltInTypeKind.Bool),
                new BuiltInType(BuiltInTypeKind.Int),
                new BuiltInType(BuiltInTypeKind.String),
                new BuiltInType(BuiltInTypeKind.Object),
                new BuiltInType(BuiltInTypeKind.Array),
                new BuiltInType(BuiltInTypeKind.ResourceRef),
                #pragma warning restore 618
            };

            using var memoryStream = BuildStream(stream => TypeSerializer.Serialize(stream, builtIns));
            var stream = TypeSerializer.Deserialize(memoryStream);

            for (var i = 0; i < builtIns.Length; i++)
            {
                stream[i].Should().BeOfType<BuiltInType>();
                var deserializedBuiltIn = (BuiltInType)stream[i];

                deserializedBuiltIn.Kind.Should().Be(builtIns[i].Kind);
            }
        }

        class DeferredReference : ITypeReference
        {
            private readonly Func<ITypeReference> typeFunc;

            public DeferredReference(Func<ITypeReference> typeFunc)
            {
                this.typeFunc = typeFunc;
            }

            public TypeBase Type => typeFunc().Type;
        }

        [TestMethod]
        public void Circular_references_are_allowed()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            ObjectType? typeA = null;
            ObjectType? typeB = null;

            typeA = factory.Create(() => new ObjectType("typeA", new Dictionary<string, ObjectTypeProperty> {
                ["typeB"] = new ObjectTypeProperty(new DeferredReference(() => factory.GetReference(typeB!)), ObjectTypePropertyFlags.None, "hello!"),
            }, null));
            typeB = factory.Create(() => new ObjectType("typeB", new Dictionary<string, ObjectTypeProperty> {
                ["typeA"] = new ObjectTypeProperty(factory.GetReference(typeA), ObjectTypePropertyFlags.None, ""),
            }, null));

            using var stream = BuildStream(stream => TypeSerializer.Serialize(stream, factory.GetTypes()));
            var deserialized = TypeSerializer.Deserialize(stream);

            deserialized[0].Should().BeOfType<ObjectType>();
            deserialized[1].Should().BeOfType<ObjectType>();

            var deserializedTypeA = (ObjectType)deserialized[0];
            var deserializedTypeB = (ObjectType)deserialized[1];

            deserializedTypeA.Properties!["typeB"].Type!.Type.Should().Be(deserializedTypeB);
            deserializedTypeB.Properties!["typeA"].Type!.Type.Should().Be(deserializedTypeA);
        }

        [TestMethod]
        public void Different_types_can_be_serialized_and_deserialized()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());

            #pragma warning disable 618
            var builtInType = factory.Create(() => new BuiltInType(BuiltInTypeKind.Int));
            #pragma warning restore 618
            var objectType = factory.Create(() => new ObjectType("steven", new Dictionary<string, ObjectTypeProperty>(), null));
            var arrayType = factory.Create(() => new ArrayType(factory.GetReference(objectType)));
            var stringType = factory.Create(() => new StringType(true, 3, 10, "^foo"));
            var functionParam = new FunctionParameter("arg", factory.GetReference(stringType), null);
            var resourceMethodType = factory.Create(() => new FunctionType(new [] { functionParam }, factory.GetReference(stringType)));
            var resourceType = factory.Create(() => new ResourceType("gerrard", ScopeType.ResourceGroup|ScopeType.Tenant, ScopeType.Tenant, factory.GetReference(objectType), ResourceFlags.None, new Dictionary<string, ResourceTypeFunction> { ["sayHi"] = new(factory.GetReference(resourceMethodType), null) }));
            var unionType = factory.Create(() => new UnionType(new [] { factory.GetReference(builtInType), factory.GetReference(objectType) }));
            var stringLiteralType = factory.Create(() => new StringLiteralType("abcdef"));
            var discriminatedObjectType = factory.Create(() => new DiscriminatedObjectType("disctest", "disctest", new Dictionary<string, ObjectTypeProperty>(), new Dictionary<string, ITypeReference>()));
            var resourceFunctionType = factory.Create(() => new ResourceFunctionType("listTest", "zona", "2020-01-01", factory.GetReference(objectType), factory.GetReference(objectType)));
            var anyType = factory.Create(() => new AnyType());
            var nullType = factory.Create(() => new NullType());
            var booleanType = factory.Create(() => new BooleanType());
            var intType = factory.Create(() => new IntegerType(-10, 10));
            var sensitiveObjectType = factory.Create(() => new ObjectType("larry", new Dictionary<string, ObjectTypeProperty>(), null, sensitive: true));

            using var stream = BuildStream(stream => TypeSerializer.Serialize(stream, factory.GetTypes()));
            var deserialized = TypeSerializer.Deserialize(stream);

            var builtInTypeDeserialized = deserialized[0].Should().BeOfType<BuiltInType>().Subject;
            var objectTypeDeserialized = deserialized[1].Should().BeOfType<ObjectType>().Subject;
            var arrayTypeDeserialized = deserialized[2].Should().BeOfType<ArrayType>().Subject;
            var stringTypeDeserialized = deserialized[3].Should().BeOfType<StringType>().Subject;
            var resourceMethodTypeDeserialized = deserialized[4].Should().BeOfType<FunctionType>().Subject;
            var resourceTypeDeserialized = deserialized[5].Should().BeOfType<ResourceType>().Subject;
            var unionTypeDeserialized = deserialized[6].Should().BeOfType<UnionType>().Subject;
            var stringLiteralTypeDeserialized = deserialized[7].Should().BeOfType<StringLiteralType>().Subject;
            var discriminatedObjectTypeDeserialized = deserialized[8].Should().BeOfType<DiscriminatedObjectType>().Subject;
            var apiAgnosticResourceFunctionTypeDeserialized = deserialized[9].Should().BeOfType<ResourceFunctionType>().Subject;
            var anyTypeDeserialized = deserialized[10].Should().BeOfType<AnyType>().Subject;
            var nullTypeDeserialized = deserialized[11].Should().BeOfType<NullType>().Subject;
            var booleanTypeDeserialized = deserialized[12].Should().BeOfType<BooleanType>().Subject;
            var integerTypeDeserialized = deserialized[13].Should().BeOfType<IntegerType>().Subject;
            var sensitiveObjectTypeDeserialized = deserialized[14].Should().BeOfType<ObjectType>().Subject;

            builtInTypeDeserialized.Kind.Should().Be(builtInType.Kind);
            objectTypeDeserialized.Name.Should().Be(objectType.Name);
            objectTypeDeserialized.Sensitive.Should().Be(objectType.Sensitive);
            arrayTypeDeserialized.ItemType!.Type.Should().Be(objectTypeDeserialized);
            stringTypeDeserialized.Sensitive.Should().BeTrue();
            stringTypeDeserialized.MinLength.Should().Be(3);
            stringTypeDeserialized.MaxLength.Should().Be(10);
            stringTypeDeserialized.Pattern.Should().Be("^foo");
            resourceTypeDeserialized.Name.Should().Be(resourceType.Name);
            resourceTypeDeserialized.Flags.Should().Be(resourceType.Flags);
            resourceTypeDeserialized.ReadOnlyScopes.HasValue.Should().Be(true);
            resourceTypeDeserialized.ReadOnlyScopes.Should().Be(resourceType.ReadOnlyScopes);
            resourceTypeDeserialized.Functions!["sayHi"].Type.Type.Should().Be(resourceMethodTypeDeserialized);
            unionTypeDeserialized.Elements![0].Type.Should().Be(builtInTypeDeserialized);
            unionTypeDeserialized.Elements![1].Type.Should().Be(objectTypeDeserialized);
            stringLiteralTypeDeserialized.Value.Should().Be(stringLiteralType.Value);
            discriminatedObjectTypeDeserialized.Name.Should().Be(discriminatedObjectType.Name);
            apiAgnosticResourceFunctionTypeDeserialized.Name.Should().Be(resourceFunctionType.Name);
            integerTypeDeserialized.MinValue.Should().Be(-10);
            integerTypeDeserialized.MaxValue.Should().Be(10);
            sensitiveObjectTypeDeserialized.Name.Should().Be(sensitiveObjectType.Name);
            sensitiveObjectTypeDeserialized.Sensitive.Should().BeTrue();
        }

        [TestMethod]
        public void Resources_without_flags_or_readonly_scopes_can_be_deserialized()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("steven", new Dictionary<string, ObjectTypeProperty>(), null));
            var resourceType = factory.Create(() => new ResourceType("gerrard", ScopeType.ResourceGroup|ScopeType.Tenant, ScopeType.Tenant, factory.GetReference(objectType), ResourceFlags.ReadOnly, null));

            using var stream = BuildStream(stream => TypeSerializer.Serialize(stream, factory.GetTypes()));
            var deserializedNode = JsonSerializer.Deserialize<JsonNode>(stream)!;
            deserializedNode.AsArray()[1]?.AsObject().Remove("flags").Should().BeTrue();
            deserializedNode.AsArray()[1]?.AsObject().Remove("readOnlyScopes").Should().BeTrue();
            using var rewrittenStream = BuildStream(stream => JsonSerializer.Serialize(stream, deserializedNode));

            var deserialized = TypeSerializer.Deserialize(rewrittenStream);

            deserialized[0].Should().BeOfType<ObjectType>();
            deserialized[1].Should().BeOfType<ResourceType>();

            ((ObjectType)deserialized[0]).Name.Should().Be(objectType.Name);
            ((ResourceType)deserialized[1]).Name.Should().Be(resourceType.Name);
            ((ResourceType)deserialized[1]).Flags.Should().Be(ResourceFlags.None);
            ((ResourceType)deserialized[1]).ReadOnlyScopes.HasValue.Should().Be(false);
        }

        private static Stream BuildStream(Action<Stream> writeFunc)
        {
            var memoryStream = new MemoryStream();
            writeFunc(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
