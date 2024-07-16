// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.UnitTests;

[TestClass]
public class CrossFileTypeReferenceConverterTests
{
    [TestMethod]
    public void Reference_conversion_uses_invariant_culture()
    {
        var index = BinaryData.FromString("""
{
  "resources": {
    "Microsoft.Addons/supportProviders/supportPlanTypes@2017-05-15": {
      "$ref": "addons/microsoft.addons/2017-05-15/types.json#/17"
    }
  },
  "resourceFunctions": {}
}
""");

        // see https://github.com/Azure/bicep/issues/14563 for more context
        Thread.CurrentThread.CurrentCulture = new CultureInfo("th-TH");
        var test =  TypeSerializer.DeserializeIndex(index.ToStream());

        test.Resources.Single().Value.RelativePath.Should().Be("addons/microsoft.addons/2017-05-15/types.json");
        test.Resources.Single().Value.Index.Should().Be(17);
    }
}
