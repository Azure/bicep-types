// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class ReferencePlacementTests
{
    [TestMethod]
    [DataRow("Index", "types.json", true)]
    [DataRow("Index", "common/types.json", true)]
    [DataRow("Index", "", false)]
    [DataRow("TypeFile", "", true)]
    [DataRow("TypeFile", "types.json", false)]
    [DataRow("TypeFile", "common/types.json", false)]
    public void Placement_depends_on_document_kind_and_path_emptiness(
        string documentKind,
        string canonicalPackagePath,
        bool expected)
    {
        var kind = Enum.Parse<PackageDocumentKind>(documentKind);

        ReferencePlacement.IsAllowed(kind, canonicalPackagePath)
            .Should().Be(expected);
    }

    [TestMethod]
    public void Unknown_document_kind_is_rejected()
    {
        Action act = () => ReferencePlacement.IsAllowed((PackageDocumentKind)999, "types.json");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}