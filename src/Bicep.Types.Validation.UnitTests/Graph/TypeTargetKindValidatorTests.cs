// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Bicep.Types.Validation.Graph;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Graph;

[TestClass]
public class TypeTargetKindValidatorTests
{
    // ── IsTopLevel ───────────────────────────────────────────────────────────

    [TestMethod]
    [DataRow("ResourceRoot", true)]
    [DataRow("ResourceFunctionRoot", true)]
    [DataRow("NamespaceFunctionRoot", true)]
    [DataRow("FallbackResourceType", true)]
    [DataRow("ConfigurationType", true)]
    [DataRow("ResourceBody", false)]
    [DataRow("ObjectPropertyType", false)]
    [DataRow("ArrayItem", false)]
    public void IsTopLevel_classifies_root_roles(string role, bool expected)
    {
        TypeTargetKindValidator.IsTopLevel(Role(role)).Should().Be(expected);
    }

    // ── IsAllowed: container roles ───────────────────────────────────────────

    [TestMethod]
    [DataRow("ResourceRoot", "ResourceType", true)]
    [DataRow("ResourceRoot", "ObjectType", false)]
    [DataRow("FallbackResourceType", "ResourceType", true)]
    [DataRow("ResourceFunctionRoot", "ResourceFunctionType", true)]
    [DataRow("ResourceFunctionRoot", "FunctionType", false)]
    [DataRow("NamespaceFunctionRoot", "NamespaceFunctionType", true)]
    [DataRow("ResourceBody", "ObjectType", true)]
    [DataRow("ResourceBody", "DiscriminatedObjectType", true)]
    [DataRow("ResourceBody", "StringType", false)]
    [DataRow("ConfigurationType", "ObjectType", true)]
    [DataRow("ResourceTypeFunction", "FunctionType", true)]
    [DataRow("ResourceTypeFunction", "ObjectType", false)]
    [DataRow("DiscriminatedObjectElement", "ObjectType", true)]
    [DataRow("DiscriminatedObjectElement", "DiscriminatedObjectType", false)]
    public void IsAllowed_enforces_container_role_targets(string role, string discriminator, bool expected)
    {
        TypeTargetKindValidator.IsAllowed(Role(role), discriminator).Should().Be(expected);
    }

    // ── IsAllowed: value-type roles ──────────────────────────────────────────

    [TestMethod]
    [DataRow("StringType", true)]
    [DataRow("IntegerType", true)]
    [DataRow("ObjectType", true)]
    [DataRow("UnionType", true)]
    [DataRow("BuiltInType", true)]
    [DataRow("ResourceType", false)]
    [DataRow("ResourceFunctionType", false)]
    [DataRow("NamespaceFunctionType", false)]
    [DataRow("FunctionType", false)]
    public void IsAllowed_value_role_accepts_only_value_types(string discriminator, bool expected)
    {
        TypeTargetKindValidator.IsAllowed(TypeReferenceRole.ObjectPropertyType, discriminator)
            .Should().Be(expected);
    }

    // ── ExpectedText ─────────────────────────────────────────────────────────

    [TestMethod]
    public void ExpectedText_describes_expected_kinds()
    {
        TypeTargetKindValidator.ExpectedText(TypeReferenceRole.ResourceRoot)
            .Should().Be("a resource type ('ResourceType')");
        TypeTargetKindValidator.ExpectedText(TypeReferenceRole.ResourceBody)
            .Should().Be("an object type ('ObjectType' or 'DiscriminatedObjectType')");
        TypeTargetKindValidator.ExpectedText(TypeReferenceRole.ObjectPropertyType)
            .Should().Be("a value type");
    }

    private static TypeReferenceRole Role(string name) =>
        (TypeReferenceRole)Enum.Parse(typeof(TypeReferenceRole), name);
}
