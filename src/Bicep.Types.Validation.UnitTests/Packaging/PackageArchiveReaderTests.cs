// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class PackageArchiveReaderTests
{
    private const string MinimalIndexJson = @"{
  ""resources"": {},
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";

    [TestMethod]
    public void PackageInputResolver_archive_file_preserves_display_path_and_physical_path()
    {
        var resolution = PackageInputResolver.Resolve(
            TypePackageValidationInput.ForArchiveFile("some/dir/types.tgz"));

        resolution.Kind.Should().Be(PackageInputKind.ArchiveFile);
        resolution.ArchiveFilePath.Should().Be("some/dir/types.tgz");
        resolution.DisplayPath.Should().Contain("types.tgz");
        resolution.ArchiveBytes.Should().BeNull();
    }

    [TestMethod]
    public void PackageInputResolver_archive_stream_preserves_display_path_and_stream()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        using var stream = new MemoryStream(payload);

        var resolution = PackageInputResolver.Resolve(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        resolution.Kind.Should().Be(PackageInputKind.ArchiveStream);
        resolution.DisplayPath.Should().Be("types.tgz");
        resolution.ArchiveBytes.Should().Equal(payload);
        // The caller's stream is read fully but not disposed.
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public void PackageReader_archive_input_returns_archive_file_system()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);
        var resolution = PackageInputResolver.Resolve(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        var result = PackageReader.Read(resolution, new TypePackageValidationOptions());

        result.HasFatalReadFailure.Should().BeFalse();
        result.FileSystem.Should().BeOfType<ArchivePackageFileSystem>();
        result.Documents.IndexDocument.Should().NotBeNull();
    }

    [TestMethod]
    public void PackageReader_archive_read_failure_is_fatal()
    {
        using var stream = new MemoryStream(new byte[] { 9, 9, 9, 9 });
        var resolution = PackageInputResolver.Resolve(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        var result = PackageReader.Read(resolution, new TypePackageValidationOptions());

        result.HasFatalReadFailure.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchivePackageInvalid);
    }
}
