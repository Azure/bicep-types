// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Validation;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Graph;

[TestClass]
public class TypeReferenceResolverTests
{
    private const string IndexJson =
        "{\"resources\":{},\"resourceFunctions\":{},\"namespaceFunctions\":[]}";

    private static TypeReferenceResolver CreateResolver(InMemoryPackageFileSystem fs)
    {
        var index = GraphTestHelpers.Document("index.json", IndexJson);
        var provider = new PackageDocumentProvider(fs, index, new TypePackageValidationOptions());
        return new TypeReferenceResolver(provider);
    }

    private static ParsedTypeReference Ref(string targetPath, int index, string sourcePath = "index.json") =>
        new ParsedTypeReference(
            $"{targetPath}#/{index}", targetPath, index, sourcePath, "/x/$ref", new SourceLocation(1, 1));

    [TestMethod]
    public void Resolve_returns_resolved_for_usable_target()
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json", "[{\"$type\":\"StringType\"}]");
        var resolution = CreateResolver(fs).Resolve(Ref("types.json", 0));

        resolution.Outcome.Should().Be(TypeReferenceResolutionOutcome.Resolved);
        resolution.TargetNode!.Discriminator.Should().Be("StringType");
    }

    [TestMethod]
    public void Provider_uses_one_cached_identity_for_path_aliases()
    {
        var fs = new InMemoryPackageFileSystem().AddText("common/types.json", "[{\"$type\":\"StringType\"}]");
        var index = GraphTestHelpers.Document("index.json", IndexJson);
        var provider = new PackageDocumentProvider(fs, index, new TypePackageValidationOptions());

        var canonical = provider.GetTypeFile("common/types.json");
        var dotAlias = provider.GetTypeFile("./common/./types.json");
        var separatorAlias = provider.GetTypeFile(@"common\\types.json");

        dotAlias.Should().BeSameAs(canonical);
        separatorAlias.Should().BeSameAs(canonical);
        canonical.Document!.PackageRelativePath.Should().Be("common/types.json");
        provider.GetReachedFilePaths().Should().BeEquivalentTo("index.json", "common/types.json");
    }

    [TestMethod]
    public void Provider_rejects_invalid_internal_path()
    {
        var fs = new InMemoryPackageFileSystem();
        var index = GraphTestHelpers.Document("index.json", IndexJson);
        var provider = new PackageDocumentProvider(fs, index, new TypePackageValidationOptions());

        System.Action act = () => provider.GetTypeFile("../types.json");

        act.Should().Throw<System.ArgumentException>();
    }

    [TestMethod]
    public void Resolve_returns_resolved_for_same_file_reference()
    {
        var fs = new InMemoryPackageFileSystem()
            .AddText("types.json", "[{\"$type\":\"StringType\"},{\"$type\":\"BooleanType\"}]");
        // Empty target path + source types.json => same-file reference.
        var resolution = CreateResolver(fs).Resolve(Ref(string.Empty, 1, sourcePath: "types.json"));

        resolution.Outcome.Should().Be(TypeReferenceResolutionOutcome.Resolved);
        resolution.TargetNode!.Discriminator.Should().Be("BooleanType");
    }

    [TestMethod]
    public void Resolve_returns_missing_file_for_unknown_target()
    {
        var fs = new InMemoryPackageFileSystem();
        var resolution = CreateResolver(fs).Resolve(Ref("nope.json", 0));

        resolution.Outcome.Should().Be(TypeReferenceResolutionOutcome.MissingFile);
        resolution.TargetPath.Should().Be("nope.json");
    }

    [TestMethod]
    public void Resolve_returns_read_failed_for_unreadable_target()
    {
        var fs = new InMemoryPackageFileSystem().AddUnreadable("bad.json");
        var resolution = CreateResolver(fs).Resolve(Ref("bad.json", 0));

        resolution.Outcome.Should().Be(TypeReferenceResolutionOutcome.FileReadFailed);
        resolution.ReadError.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void Resolve_returns_unusable_for_non_array_target()
    {
        var fs = new InMemoryPackageFileSystem().AddText("obj.json", "{}");
        var resolution = CreateResolver(fs).Resolve(Ref("obj.json", 0));

        resolution.Outcome.Should().Be(TypeReferenceResolutionOutcome.FileUnusable);
    }

    [TestMethod]
    public void Resolve_returns_out_of_range_for_index_past_end()
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json", "[{\"$type\":\"StringType\"}]");
        var resolution = CreateResolver(fs).Resolve(Ref("types.json", 5));

        resolution.Outcome.Should().Be(TypeReferenceResolutionOutcome.IndexOutOfRange);
        resolution.TargetElementCount.Should().Be(1);
    }

    [TestMethod]
    public void Resolve_returns_not_type_object_for_unusable_element()
    {
        var fs = new InMemoryPackageFileSystem().AddText("types.json", "[42]");
        var resolution = CreateResolver(fs).Resolve(Ref("types.json", 0));

        resolution.Outcome.Should().Be(TypeReferenceResolutionOutcome.TargetNotTypeObject);
    }
}
