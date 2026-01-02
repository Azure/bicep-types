// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
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
            var resourceType = factory.Create(() => new ResourceType("gerrard", factory.GetReference(objectType), new Dictionary<string, ResourceTypeFunction> { ["sayHi"] = new(factory.GetReference(resourceMethodType), null) }, writableScopes_in: ScopeType.ResourceGroup, readableScopes_in: ScopeType.ResourceGroup));
            var unionType = factory.Create(() => new UnionType(new [] { factory.GetReference(builtInType), factory.GetReference(objectType) }));
            var stringLiteralType = factory.Create(() => new StringLiteralType("abcdef"));
            var discriminatedObjectType = factory.Create(() => new DiscriminatedObjectType("disctest", "disctest", new Dictionary<string, ObjectTypeProperty>(), new Dictionary<string, ITypeReference>()));
            var resourceFunctionType = factory.Create(() => new ResourceFunctionType("listTest", "zona", "2020-01-01", factory.GetReference(objectType), factory.GetReference(objectType)));
            var anyType = factory.Create(() => new AnyType());
            var namespaceFunctionType = factory.Create(() => new NamespaceFunctionType("binding", null, "[externalInputs('binding', parameters('bindingKey'))]", [new FunctionParameter("bindingKey", factory.GetReference(stringType), null)], factory.GetReference(anyType), NamespaceFunctionTypeFlags.ExternalInput, NamespaceFunctionTypeFileVisibility.Bicepparam));
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
            var namespaceFunctionTypeDeserialized = deserialized[10].Should().BeOfType<NamespaceFunctionType>().Subject;
            var anyTypeDeserialized = deserialized[11].Should().BeOfType<AnyType>().Subject;
            var nullTypeDeserialized = deserialized[12].Should().BeOfType<NullType>().Subject;
            var booleanTypeDeserialized = deserialized[13].Should().BeOfType<BooleanType>().Subject;
            var integerTypeDeserialized = deserialized[14].Should().BeOfType<IntegerType>().Subject;
            var sensitiveObjectTypeDeserialized = deserialized[15].Should().BeOfType<ObjectType>().Subject;

            builtInTypeDeserialized.Kind.Should().Be(builtInType.Kind);
            objectTypeDeserialized.Name.Should().Be(objectType.Name);
            objectTypeDeserialized.Sensitive.Should().Be(objectType.Sensitive);
            arrayTypeDeserialized.ItemType!.Type.Should().Be(objectTypeDeserialized);
            stringTypeDeserialized.Sensitive.Should().BeTrue();
            stringTypeDeserialized.MinLength.Should().Be(3);
            stringTypeDeserialized.MaxLength.Should().Be(10);
            stringTypeDeserialized.Pattern.Should().Be("^foo");
            resourceTypeDeserialized.Name.Should().Be(resourceType.Name);
            resourceTypeDeserialized.Functions!["sayHi"].Type.Type.Should().Be(resourceMethodTypeDeserialized);
            unionTypeDeserialized.Elements![0].Type.Should().Be(builtInTypeDeserialized);
            unionTypeDeserialized.Elements![1].Type.Should().Be(objectTypeDeserialized);
            stringLiteralTypeDeserialized.Value.Should().Be(stringLiteralType.Value);
            discriminatedObjectTypeDeserialized.Name.Should().Be(discriminatedObjectType.Name);
            apiAgnosticResourceFunctionTypeDeserialized.Name.Should().Be(resourceFunctionType.Name);
            namespaceFunctionTypeDeserialized.Name.Should().Be(namespaceFunctionType.Name);
            namespaceFunctionTypeDeserialized.Flags.Should().Be(NamespaceFunctionTypeFlags.ExternalInput);
            namespaceFunctionTypeDeserialized.FileVisibilityFlags.Should().Be(NamespaceFunctionTypeFileVisibility.Bicepparam);
            integerTypeDeserialized.MinValue.Should().Be(-10);
            integerTypeDeserialized.MaxValue.Should().Be(10);
            sensitiveObjectTypeDeserialized.Name.Should().Be(sensitiveObjectType.Name);
            sensitiveObjectTypeDeserialized.Sensitive.Should().BeTrue();
        }

        [TestMethod]
        public void ResourceType_with_readable_and_writable_scopes_can_be_serialized_and_deserialized()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("sampleObject", new Dictionary<string, ObjectTypeProperty>(), null));
            var resourceType = factory.Create(() => new ResourceType(
                "test.resourceType",
                factory.GetReference(objectType),
                null,
                readableScopes_in: ScopeType.ResourceGroup | ScopeType.ManagementGroup,
                writableScopes_in: ScopeType.ResourceGroup));

            using var stream = BuildStream(stream => TypeSerializer.Serialize(stream, factory.GetTypes()));
            var deserialized = TypeSerializer.Deserialize(stream);

            var deserializedResource = deserialized.Single(t => t is ResourceType) as ResourceType;

            deserializedResource!.ReadableScopes.Should().Be(ScopeType.ResourceGroup | ScopeType.ManagementGroup);
            deserializedResource!.WritableScopes.Should().Be(ScopeType.ResourceGroup);
        }

        [TestMethod]
        public void Mixed_legacy_and_modern_scope_fields_throws_error()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("sampleObject", new Dictionary<string, ObjectTypeProperty>(), null));

            Action act = () =>
            {
                _ = new ResourceType(
                    name: "test.resource",
                    body: factory.GetReference(objectType),
                    functions: null,
                    scopeType: ScopeType.None,
                    readOnlyScopes: ScopeType.Subscription,
                    flags: ResourceFlags.None,
                    readableScopes_in: ScopeType.ResourceGroup,
                    writableScopes_in: ScopeType.ResourceGroup);
            };

            act.Should().Throw<ArgumentException>().WithMessage("*Cannot mix*");
        }

        [TestMethod]
        public void Legacy_only_resourceType_outputs_modern_fields_in_json()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("sampleObject", new Dictionary<string, ObjectTypeProperty>(), null));

            var resourceType  = new ResourceType(
                "test.resource",
                factory.GetReference(objectType),
                null,
                scopeType: ScopeType.ResourceGroup,
                flags: ResourceFlags.None);

            var serializedJson = JsonSerializer.Serialize(resourceType, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            var json = JsonSerializer.Deserialize<JsonObject>(serializedJson)!;

            // Legacy input should be normalized to modern output
            json.ContainsKey("scopeType").Should().BeFalse();        
            json.ContainsKey("writableScopes").Should().BeTrue();
            json.ContainsKey("readableScopes").Should().BeTrue();
        }

        [TestMethod]
        public void Modern_only_resourceType_omits_legacy_fields_in_json()
        {
            var factory    = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("sampleObject", new Dictionary<string, ObjectTypeProperty>(), null));

            var resourceType = new ResourceType(
                name: "test.resource",
                body: factory.GetReference(objectType),
                functions: null,
                writableScopes_in: ScopeType.ResourceGroup,
                readableScopes_in: ScopeType.ResourceGroup | ScopeType.ManagementGroup);

            var serializedJson = JsonSerializer.Serialize(resourceType, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            var json = JsonSerializer.Deserialize<JsonObject>(serializedJson)!;

            json.ContainsKey("writableScopes").Should().BeTrue();
            json.ContainsKey("readableScopes").Should().BeTrue();

            json.ContainsKey("scopeType").Should().BeFalse();
            json.ContainsKey("readOnlyScopes").Should().BeFalse();
            json.ContainsKey("flags").Should().BeFalse();
        }

        [TestMethod]
        public void ResourceType_without_any_writable_or_readable_source_defaults_to_none()
        {
            var factory    = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("sampleObject", new Dictionary<string, ObjectTypeProperty>(), null));

            var resourceType = new ResourceType(
                name: "test.resource",
                body: factory.GetReference(objectType),
                functions: null);

            // When no scope parameters are provided, should default to None scopes
            resourceType.WritableScopes.Should().Be(ScopeType.None);
            resourceType.ReadableScopes.Should().Be(ScopeType.None);
        }

        [TestMethod]
        public void Legacy_resourceType_with_readonly_flag_sets_writable_to_none()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("sampleObject", new Dictionary<string, ObjectTypeProperty>(), null));

            var resourceType = new ResourceType(
                "readonly.resource",
                factory.GetReference(objectType),
                null,
                scopeType: ScopeType.ResourceGroup,
                flags: ResourceFlags.ReadOnly);

            // ReadOnly flag should make WritableScopes = None
            resourceType.ReadableScopes.Should().Be(ScopeType.ResourceGroup);
            resourceType.WritableScopes.Should().Be(ScopeType.None);
        }

        [TestMethod]
        public void Legacy_resourceType_with_readonly_scopes_expands_readable()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("sampleObject", new Dictionary<string, ObjectTypeProperty>(), null));

            var resourceType = new ResourceType(
                "test.resource", 
                factory.GetReference(objectType),
                null,
                scopeType: ScopeType.ResourceGroup,
                readOnlyScopes: ScopeType.Tenant,
                flags: ResourceFlags.None);

            // ReadableScopes should combine scopeType + readOnlyScopes using |= operator
            resourceType.ReadableScopes.Should().Be(ScopeType.ResourceGroup | ScopeType.Tenant);
            resourceType.WritableScopes.Should().Be(ScopeType.ResourceGroup);
        }

        [TestMethod]
        public void Legacy_json_deserializes_and_reserializes_to_modern_format()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("testObject", new Dictionary<string, ObjectTypeProperty>(), null));
            
            var legacyResource = new ResourceType(
                "test.resource",
                factory.GetReference(objectType),
                null,
                scopeType: ScopeType.ResourceGroup,
                readOnlyScopes: ScopeType.Tenant,
                flags: ResourceFlags.None);

            var serializedJson = JsonSerializer.Serialize(legacyResource, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            serializedJson.Should().NotContain("scopeType");
            serializedJson.Should().NotContain("readOnlyScopes");
            serializedJson.Should().NotContain("flags");
            
            serializedJson.Should().Contain("readableScopes");
            serializedJson.Should().Contain("writableScopes");

            // Verify the actual scope values are correct (scopeType | readOnlyScopes)
            legacyResource.ReadableScopes.Should().Be(ScopeType.ResourceGroup | ScopeType.Tenant);
            legacyResource.WritableScopes.Should().Be(ScopeType.ResourceGroup);
        }

        [TestMethod]
        public void Legacy_with_all_fields_combines_correctly()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var objectType = factory.Create(() => new ObjectType("sampleObject", new Dictionary<string, ObjectTypeProperty>(), null));

            var resourceType = new ResourceType(
                "test.resource",
                factory.GetReference(objectType), 
                null,
                scopeType: ScopeType.ResourceGroup,
                readOnlyScopes: ScopeType.Tenant | ScopeType.ManagementGroup,
                flags: ResourceFlags.ReadOnly);

            resourceType.ReadableScopes.Should().Be(ScopeType.ResourceGroup | ScopeType.Tenant | ScopeType.ManagementGroup);
            resourceType.WritableScopes.Should().Be(ScopeType.None);
        }

        [TestMethod]
        public void ResourceType_with_scopes_deserializes_correctly_from_json()
        {
            // This test loads from JSON string to match customer scenario (AOT Team)
            var json = @"[
                {
                    ""$type"": ""ObjectType"",
                    ""name"": ""sampleBody"",
                    ""properties"": {}
                },
                {
                    ""$type"": ""ResourceType"",
                    ""name"": ""Microsoft.ApiManagement/service/diagnostics/loggers"",
                    ""body"": {
                        ""$ref"": ""#/0""
                    },
                    ""scopeType"": 8,
                    ""flags"": 0,
                    ""readOnlyScopes"": null
                }
            ]";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            
            // Deserialize directly from JSON, thiss matches how AzTypeLoader would read the file
            var deserialized = TypeSerializer.Deserialize(stream);
            var deserializedResource = deserialized.OfType<ResourceType>().Single();
            
            deserializedResource.ReadableScopes.Should().Be(ScopeType.ResourceGroup);
            deserializedResource.WritableScopes.Should().Be(ScopeType.ResourceGroup);
        }

        [TestMethod]
        public void ResourceType_with_legacy_unknown_scope_deserializes_as_all()
        {
            // This test verifies that legacy ScopeType.Unknown (value 0) is remapped to ScopeType.All (all scopes including extension)
            var json = @"[
                {
                    ""$type"": ""ObjectType"",
                    ""name"": ""sampleBody"",
                    ""properties"": {}
                },
                {
                    ""$type"": ""ResourceType"",
                    ""name"": ""Microsoft.Test/legacyUnknownScopeResource"",
                    ""body"": {
                        ""$ref"": ""#/0""
                    },
                    ""scopeType"": 0,
                    ""flags"": 0,
                    ""readOnlyScopes"": null
                }
            ]";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            
            var deserialized = TypeSerializer.Deserialize(stream);
            var deserializedResource = deserialized.OfType<ResourceType>().Single();
            
            // Legacy ScopeType.Unknown (0) should be interpreted as All for backward compatibility
            deserializedResource.ReadableScopes.Should().Be(ScopeType.All);
            deserializedResource.WritableScopes.Should().Be(ScopeType.All);
        }

        [TestMethod]
        public void ResourceType_with_modern_scopes_should_not_serialize_null_legacy_properties()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var stringType = factory.Create(() => new StringType());
            var objectType = factory.Create(() => new ObjectType(
                name: "request@v1",
                properties: new Dictionary<string, ObjectTypeProperty>
                {
                    ["uri"] = new(factory.GetReference(stringType), ObjectTypePropertyFlags.Required, "The HTTP request URI")
                },
                additionalProperties: null));

            // Create ResourceType with ONLY modern properties
            var resourceType = factory.Create(() => new ResourceType(
                name: "request@v1",
                body: factory.GetReference(objectType),
                functions: null,
                writableScopes_in: ScopeType.All,
                readableScopes_in: ScopeType.All));

            // Serialize
            string serializedJson;
            using (var stream = new MemoryStream())
            {
                TypeSerializer.Serialize(stream, factory.GetTypes());
                serializedJson = Encoding.UTF8.GetString(stream.ToArray());
            }

            var doc = JsonDocument.Parse(serializedJson);
            var resourceElement = doc.RootElement.EnumerateArray()
                .First(e => e.TryGetProperty("$type", out var typeVal) && typeVal.GetString() == "ResourceType");

            // Check for legacy properties
            bool hasScopeType = resourceElement.TryGetProperty("scopeType", out _);
            bool hasReadOnlyScopes = resourceElement.TryGetProperty("readOnlyScopes", out _);
            bool hasFlags = resourceElement.TryGetProperty("flags", out _);

            // Verify modern properties are present
            bool hasReadableScopes = resourceElement.TryGetProperty("readableScopes", out var readableVal);
            bool hasWritableScopes = resourceElement.TryGetProperty("writableScopes", out var writableVal);

            // Assert: Modern properties should be present
            Assert.IsTrue(hasReadableScopes, "readableScopes should be present in JSON");
            Assert.IsTrue(hasWritableScopes, "writableScopes should be present in JSON");
            Assert.AreEqual(31, readableVal.GetInt32(), "readableScopes should be 31 (All)");
            Assert.AreEqual(31, writableVal.GetInt32(), "writableScopes should be 31 (All)");

            // Assert: Legacy properties should NOT be present (they are null and should be omitted)
            Assert.IsFalse(hasScopeType, 
                "scopeType should not be serialized when null (JsonIgnoreCondition.WhenWritingNull)");
            Assert.IsFalse(hasReadOnlyScopes, 
                "readOnlyScopes should not be serialized when null (JsonIgnoreCondition.WhenWritingNull)");
            Assert.IsFalse(hasFlags, 
                "flags should not be serialized when null (JsonIgnoreCondition.WhenWritingNull)");
        }

        [TestMethod]
        public void ResourceType_with_legacy_scopes_should_serialize_and_deserialize_correctly()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var stringType = factory.Create(() => new StringType());
            var objectType = factory.Create(() => new ObjectType(
                name: "legacy@v1",
                properties: new Dictionary<string, ObjectTypeProperty>
                {
                    ["id"] = new(factory.GetReference(stringType), ObjectTypePropertyFlags.Required, "Resource ID")
                },
                additionalProperties: null));

            // Create with legacy properties
            var resourceType = factory.Create(() => new ResourceType(
                name: "legacy@v1",
                body: factory.GetReference(objectType),
                functions: null,
                scopeType: ScopeType.ResourceGroup,
                readOnlyScopes: null,
                flags: ResourceFlags.None));

            // Serialize
            string serializedJson;
            using (var stream = new MemoryStream())
            {
                TypeSerializer.Serialize(stream, factory.GetTypes());
                serializedJson = Encoding.UTF8.GetString(stream.ToArray());
            }

            var doc = JsonDocument.Parse(serializedJson);
            var resourceElement = doc.RootElement.EnumerateArray()
                .First(e => e.TryGetProperty("$type", out var typeVal) && typeVal.GetString() == "ResourceType");

            // Deserialize
            TypeBase[] deserializedTypes;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedJson)))
            {
                deserializedTypes = TypeSerializer.Deserialize(stream);
            }

            var deserialized = deserializedTypes.OfType<ResourceType>().First();

            Assert.AreEqual(ScopeType.ResourceGroup, deserialized.ReadableScopes);
            Assert.AreEqual(ScopeType.ResourceGroup, deserialized.WritableScopes);
        }

        [TestMethod]
        public void ResourceType_modern_scopes_roundtrip_preserves_values()
        {
            var factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            var stringType = factory.Create(() => new StringType());
            var objectType = factory.Create(() => new ObjectType(
                name: "roundtrip@v1",
                properties: new Dictionary<string, ObjectTypeProperty>
                {
                    ["name"] = new(factory.GetReference(stringType), ObjectTypePropertyFlags.Required, "Name")
                },
                additionalProperties: null));

            // Create with modern properties
            var original = factory.Create(() => new ResourceType(
                name: "roundtrip@v1",
                body: factory.GetReference(objectType),
                functions: null,
                writableScopes_in: ScopeType.Subscription | ScopeType.ResourceGroup,
                readableScopes_in: ScopeType.All));

            // Serialize
            string serializedJson;
            using (var stream = new MemoryStream())
            {
                TypeSerializer.Serialize(stream, factory.GetTypes());
                serializedJson = Encoding.UTF8.GetString(stream.ToArray());
            }

            // Deserialize
            TypeBase[] deserializedTypes;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedJson)))
            {
                deserializedTypes = TypeSerializer.Deserialize(stream);
            }

            var deserialized = deserializedTypes.OfType<ResourceType>().First();

            // Verify scopes match exactly
            Assert.AreEqual(original.ReadableScopes, deserialized.ReadableScopes, 
                "ReadableScopes should be preserved through roundtrip");
            Assert.AreEqual(original.WritableScopes, deserialized.WritableScopes,
                "WritableScopes should be preserved through roundtrip");
            Assert.AreEqual(ScopeType.All, deserialized.ReadableScopes);
            Assert.AreEqual(ScopeType.Subscription | ScopeType.ResourceGroup, deserialized.WritableScopes);
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
