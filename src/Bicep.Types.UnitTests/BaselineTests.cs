// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.UnitTests;

[TestClass]
public class BaselineTests
{
    public static IEnumerable<object[]> GetBaselinePaths()
    {
        var baselinePaths = typeof(BaselineTypeLoader).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("baselines/"))
            .Select(n => n.Split('/')[1])
            .Distinct();

        foreach (var baselinePath in baselinePaths)
        {
            yield return new object[] { baselinePath };
        }
    }

    private class BaselineTypeLoader : TypeLoader
    {
        private readonly string manifestResourceBasePath;

        public BaselineTypeLoader(string baselineName)
        {
            manifestResourceBasePath = $"baselines/{baselineName}";
        }

        protected override Stream GetContentStreamAtPath(string path)
        {
            var fullPath = $"{manifestResourceBasePath}/{path}";

            return typeof(BaselineTypeLoader).Assembly.GetManifestResourceStream(fullPath)
                ?? throw new InvalidOperationException($"Unable to locate manifest resource at path {fullPath}");
        }
    }

    [TestMethod]
    public void GetBaselinePaths_returns_baselines()
    {
        // Sanity check to verify we're actually running through the different baselines
        GetBaselinePaths().Should().HaveCountGreaterThanOrEqualTo(2);
        GetBaselinePaths().Should().Contain(x => (string)x[0] == "http");
        GetBaselinePaths().Should().Contain(x => (string)x[0] == "foo");
    }

    [DataTestMethod]
    [DynamicData(nameof(GetBaselinePaths), DynamicDataSourceType.Method)]
    public void TypeLoader_can_load_all_types_without_throwing(string baselineName)
    {
        var typeLoader = new BaselineTypeLoader(baselineName);
        var index = typeLoader.LoadTypeIndex();
        index.Resources.Should().NotBeEmpty();

        foreach (var kvp in index.Resources)
        {
            var resourceType = typeLoader.LoadResourceType(kvp.Value);
        }

        foreach (var (resourceType, functionsByApiVersion) in index.ResourceFunctions)
        {
            foreach (var (apiVersion, resourceFunctions) in functionsByApiVersion)
            {
                foreach (var reference in resourceFunctions)
                {
                    var resourceFunctionType = typeLoader.LoadResourceFunctionType(reference);
                }
            }
        }

        if (index.Settings != null && index.Settings.ConfigurationType != null)
        {
            var objectType = typeLoader.LoadObjectType(index.Settings.ConfigurationType);
        }

        if (index.FallbackResourceType != null)
        {
            var resourceType = typeLoader.LoadResourceType(index.FallbackResourceType);
        }
    }
}
