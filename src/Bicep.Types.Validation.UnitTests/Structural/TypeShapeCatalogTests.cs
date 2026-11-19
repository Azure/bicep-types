// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Validation.Structural;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Structural;

[TestClass]
public class TypeShapeCatalogTests
{
    // The expected $type discriminators derived from TypeBase [JsonDerivedType] registrations
    private static readonly string[] ExpectedDiscriminators =
    {
        "ArrayType", "BuiltInType", "DiscriminatedObjectType", "ObjectType",
        "FunctionType", "ResourceFunctionType", "NamespaceFunctionType",
        "ResourceType", "StringLiteralType", "UnionType", "AnyType",
        "NullType", "BooleanType", "IntegerType", "StringType",
    };

    [TestMethod]
    public void Catalog_contains_every_TypeBase_derived_discriminator()
    {
        foreach (var discriminator in ExpectedDiscriminators)
        {
            TypeShapeCatalog.GetDescriptor(discriminator)
                .Should().NotBeNull(because: $"'{discriminator}' is a [JsonDerivedType] on TypeBase");
        }
    }

    [TestMethod]
    public void Catalog_count_exactly_matches_TypeBase_json_derived_type_registrations()
    {
        TypeShapeCatalog.AllDiscriminators.Should().HaveCount(ExpectedDiscriminators.Length);
    }

    [TestMethod]
    public void Catalog_excludes_scope_type_which_is_an_enum_not_a_discriminator()
    {
        // ScopeType is a flags enum used as a field value inside ResourceType, not a $type discriminator
        TypeShapeCatalog.GetDescriptor("ScopeType").Should().BeNull();
    }

    [TestMethod]
    public void All_structurally_supported_kinds_including_BuiltInType_have_descriptors()
    {
        TypeShapeCatalog.GetDescriptor("BuiltInType").Should().NotBeNull();
        TypeShapeCatalog.GetDescriptor("BuiltInType")!.Fields.Should().ContainSingle(f => f.Name == "kind");
    }

    [TestMethod]
    public void Required_field_metadata_exists_for_non_trivial_kinds()
    {
        // ObjectType has required name and properties fields
        var objectType = TypeShapeCatalog.GetDescriptor("ObjectType");
        objectType.Should().NotBeNull();
        objectType!.Fields.Where(f => f.Required).Select(f => f.Name)
            .Should().Contain(new[] { "name", "properties" });

        // ResourceType has required name, body, readableScopes, writableScopes
        var resourceType = TypeShapeCatalog.GetDescriptor("ResourceType");
        resourceType!.Fields.Where(f => f.Required).Select(f => f.Name)
            .Should().Contain(new[] { "name", "body", "readableScopes", "writableScopes" });
    }

    [TestMethod]
    public void Reference_valued_fields_are_marked_as_Ref_shape()
    {
        // ArrayType.itemType is a ref
        var arrayType = TypeShapeCatalog.GetDescriptor("ArrayType");
        arrayType!.Fields.Single(f => f.Name == "itemType").Shape.Should().Be(FieldShape.Ref);

        // ResourceType.body is a ref
        var resourceType = TypeShapeCatalog.GetDescriptor("ResourceType");
        resourceType!.Fields.Single(f => f.Name == "body").Shape.Should().Be(FieldShape.Ref);
    }

    [TestMethod]
    public void ResourceType_legacy_fields_are_marked_as_legacy_compat_only()
    {
        var resourceType = TypeShapeCatalog.GetDescriptor("ResourceType");
        var legacyFields = resourceType!.Fields.Where(f => f.LegacyCompatOnly).Select(f => f.Name);
        legacyFields.Should().Contain(new[] { "scopeType", "readOnlyScopes", "flags" });
    }
}
