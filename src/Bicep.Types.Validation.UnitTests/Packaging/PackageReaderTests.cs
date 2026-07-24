// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Azure.Bicep.Types.Validation;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class PackageReaderTests
{
    private static readonly TypePackageValidationOptions DefaultOptions = new();

    // ── Directory input ─────────────────────────────────────────────────────

    [TestMethod]
    public void Directory_input_reads_package_root_index_json()
    {
        using var pkg = CreateMinimalPackage();
        var resolution = Resolve(pkg.Path, isDirectory: true);
        var result = PackageReader.Read(resolution, DefaultOptions);

        result.HasFatalReadFailure.Should().BeFalse();
        result.Documents.IndexDocument.Should().NotBeNull();
        result.Documents.IndexDocument!.PackageRelativePath.Should().Be("index.json");
    }

    [TestMethod]
    public void Index_file_input_treats_containing_directory_as_package_root()
    {
        using var pkg = CreateMinimalPackage();
        string indexPath = Path.Combine(pkg.Path, "index.json");
        var resolution = Resolve(indexPath, isDirectory: false);
        var result = PackageReader.Read(resolution, DefaultOptions);

        result.HasFatalReadFailure.Should().BeFalse();
        result.Documents.IndexDocument.Should().NotBeNull();
    }

    [TestMethod]
    public void Nonexistent_directory_input_returns_package_path_invalid()
    {
        var resolution = Resolve("C:\\does-not-exist-xyz-abc", isDirectory: true);
        var result = PackageReader.Read(resolution, DefaultOptions);

        result.HasFatalReadFailure.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.PackagePathInvalid);
    }

    [TestMethod]
    public void Directory_input_with_missing_index_json_returns_index_file_missing()
    {
        using var emptyDir = new TempDir();
        var resolution = Resolve(emptyDir.Path, isDirectory: true);
        var result = PackageReader.Read(resolution, DefaultOptions);

        result.HasFatalReadFailure.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.IndexFileMissing);
    }

    // ── Type files are not loaded by the reader (owned by the graph provider) ─

    [TestMethod]
    public void Reader_does_not_load_type_files()
    {
        // Type-file loading and transitive closure are owned by the graph layer's
        // package document provider; the reader materializes only index.json.
        using var pkg = CreatePackageWithTypes();
        var resolution = Resolve(pkg.Path, isDirectory: true);
        var result = PackageReader.Read(resolution, DefaultOptions);

        result.HasFatalReadFailure.Should().BeFalse();
        result.Documents.TypeFiles.Should().BeEmpty();
        result.FileSystem.Should().NotBeNull();
    }

    [TestMethod]
    public void Missing_referenced_type_file_is_not_a_reader_read_failure()
    {
        // index.json references types.json but that file doesn't exist. The reader only
        // parses index.json, so it reports no read failure; the missing file is reported
        // later by graph validation (BCPVT017).
        const string indexJson = @"{
  ""resources"": { ""S/r@2026-01-01"": { ""$ref"": ""types.json#/0"" } },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";
        using var pkg = new TempDir();
        File.WriteAllText(Path.Combine(pkg.Path, "index.json"), indexJson);
        // types.json intentionally NOT created

        var resolution = Resolve(pkg.Path, isDirectory: true);
        var result = PackageReader.Read(resolution, DefaultOptions);

        result.HasFatalReadFailure.Should().BeFalse(); // index read succeeded
        result.Diagnostics.Should().BeEmpty();
    }

    // ── Archive input pass-through (handled upstream) ────────────────────────

    [TestMethod]
    public void Null_package_root_path_returns_package_path_invalid()
    {
        // A resolution with null PackageRootPath shouldn't crash the reader
        var resolution = new PackageInputResolution(
            PackageInputKind.Directory, "display",
            packageRootPath: null, indexFilePath: null,
            diagnostics: new TypeValidationDiagnostic[0]);

        var result = PackageReader.Read(resolution, DefaultOptions);

        result.HasFatalReadFailure.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.PackagePathInvalid);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PackageInputResolution Resolve(string path, bool isDirectory)
    {
        var input = isDirectory
            ? TypePackageValidationInput.ForDirectory(path)
            : TypePackageValidationInput.ForIndexFile(path);
        return PackageInputResolver.Resolve(input);
    }

    private static TempDir CreateMinimalPackage()
    {
        var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "index.json"),
            "{\"resources\":{},\"resourceFunctions\":{},\"namespaceFunctions\":[]}");
        return dir;
    }

    private static TempDir CreatePackageWithTypes()
    {
        var dir = new TempDir();
        const string indexJson = @"{
  ""resources"": { ""S/r@2026-01-01"": { ""$ref"": ""types.json#/0"" } },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";
        File.WriteAllText(Path.Combine(dir.Path, "index.json"), indexJson);
        File.WriteAllText(Path.Combine(dir.Path, "types.json"), "[{\"$type\":\"StringType\"}]");
        return dir;
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bcpvt-rdr-" + System.IO.Path.GetRandomFileName());

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best-effort */ }
        }
    }
}
