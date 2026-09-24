// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Azure.Bicep.Types.Validation.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class ArchiveInputTests
{
    private const string MinimalIndexJson = @"{
  ""resources"": {},
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";

    private static readonly TypePackageValidator Validator = new();

    [TestMethod]
    public void Archive_file_input_validates_minimal_package()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var file = new TempFile(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile(file.Path));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Archive_stream_input_validates_minimal_package()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeTrue();
        // The caller's stream is read but not disposed.
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public void Archive_input_leading_dot_slash_prefix_is_accepted()
    {
        var archive = TarGzTestArchive.FromTextFiles(("./index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void Archive_input_uses_member_paths_in_json_syntax_diagnostics()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", "this is not json"));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.JsonSyntaxInvalid)
            .Which.Path.Should().Be("index.json");
    }

    [TestMethod]
    public void Archive_input_missing_index_json_reports_bcpvt002()
    {
        var archive = TarGzTestArchive.FromTextFiles(("types.json", "[]"));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.IndexFileMissing);
    }

    [TestMethod]
    public void Archive_input_malformed_gzip_reports_bcpvt030()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchivePackageInvalid);
    }

    [TestMethod]
    public void Archive_input_malformed_tar_reports_bcpvt030()
    {
        // Valid gzip container wrapping a header block with a corrupted ustar magic marker.
        var tar = TarGzTestArchive.BuildTar(new[] { TarGzTestEntry.File("index.json", MinimalIndexJson) });
        tar[257] = (byte)'X';
        var archive = TarGzTestArchive.GzipCompress(tar);
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchivePackageInvalid);
    }

    [TestMethod]
    public void Archive_input_duplicate_member_reports_bcpvt032()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            TarGzTestEntry.File("index.json", MinimalIndexJson),
            TarGzTestEntry.File("types.json", "[]"),
            TarGzTestEntry.File("types.json", "[]"),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveMemberDuplicate);
    }

    [TestMethod]
    public void Archive_input_normalized_path_collision_reports_bcpvt033()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            TarGzTestEntry.File("index.json", MinimalIndexJson),
            TarGzTestEntry.File("a/types.json", "[]"),
            TarGzTestEntry.File("./a/types.json", "[]"),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveMemberPathCollision);
    }

    [TestMethod]
    public void Archive_input_case_only_path_collision_reports_bcpvt033()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            TarGzTestEntry.File("index.json", MinimalIndexJson),
            TarGzTestEntry.File("A/types.json", "[]"),
            TarGzTestEntry.File("a/types.json", "[]"),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveMemberPathCollision);
    }

    [TestMethod]
    public void Archive_input_absolute_member_path_reports_bcpvt031()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            TarGzTestEntry.File("index.json", MinimalIndexJson),
            TarGzTestEntry.File("/etc/types.json", "[]"),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveMemberPathInvalid);
    }

    [TestMethod]
    public void Archive_input_dotdot_member_path_reports_bcpvt031()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            TarGzTestEntry.File("index.json", MinimalIndexJson),
            TarGzTestEntry.File("../types.json", "[]"),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveMemberPathInvalid);
    }

    [TestMethod]
    public void Archive_input_backslash_member_path_reports_bcpvt031()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            TarGzTestEntry.File("index.json", MinimalIndexJson),
            TarGzTestEntry.File("a\\types.json", "[]"),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveMemberPathInvalid);
    }

    [TestMethod]
    public void Archive_input_symlink_member_reports_bcpvt031()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            TarGzTestEntry.File("index.json", MinimalIndexJson),
            TarGzTestEntry.Symlink("link.json"),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveMemberPathInvalid);
    }

    [TestMethod]
    public void Archive_input_with_resource_function_only_type_file_validates_when_member_present()
    {
        const string indexJson = @"{
  ""resources"": { ""Sample.Provider/widgets@2026-01-01"": { ""$ref"": ""types.json#/1"" } },
  ""resourceFunctions"": { ""Sample.Provider/widgets"": { ""2026-01-01"": [ { ""$ref"": ""functions.json#/0"" } ] } },
  ""namespaceFunctions"": []
}";
        const string typesJson = @"[
  { ""$type"": ""ObjectType"", ""name"": ""body"", ""properties"": {} },
  { ""$type"": ""ResourceType"", ""name"": ""Sample.Provider/widgets@2026-01-01"", ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        const string functionsJson = @"[
  { ""$type"": ""ResourceFunctionType"", ""name"": ""listSecrets"", ""resourceType"": ""Sample.Provider/widgets"", ""apiVersion"": ""2026-01-01"", ""output"": { ""$ref"": ""#/1"" }, ""input"": { ""$ref"": ""#/2"" } },
  { ""$type"": ""ObjectType"", ""name"": ""listSecretsOutput"", ""properties"": {} },
  { ""$type"": ""ObjectType"", ""name"": ""listSecretsInput"", ""properties"": {} }
]";
        var archive = TarGzTestArchive.FromTextFiles(
            ("index.json", indexJson),
            ("types.json", typesJson),
            ("functions.json", functionsJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Archive_input_missing_resource_function_only_type_file_reports_bcpvt017()
    {
        const string indexJson = @"{
  ""resources"": { ""Sample.Provider/widgets@2026-01-01"": { ""$ref"": ""types.json#/1"" } },
  ""resourceFunctions"": { ""Sample.Provider/widgets"": { ""2026-01-01"": [ { ""$ref"": ""functions.json#/0"" } ] } },
  ""namespaceFunctions"": []
}";
        const string typesJson = @"[
  { ""$type"": ""ObjectType"", ""name"": ""body"", ""properties"": {} },
  { ""$type"": ""ResourceType"", ""name"": ""Sample.Provider/widgets@2026-01-01"", ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 }
]";
        var archive = TarGzTestArchive.FromTextFiles(
            ("index.json", indexJson),
            ("types.json", typesJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferencedTypeFileMissing);
    }

    [TestMethod]
    public void Archive_and_equivalent_directory_produce_same_result()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var file = new TempFile(archive);
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "index.json"), MinimalIndexJson);

        var archiveResult = Validator.Validate(TypePackageValidationInput.ForArchiveFile(file.Path));
        var directoryResult = Validator.Validate(TypePackageValidationInput.ForDirectory(dir.Path));

        archiveResult.IsValid.Should().Be(directoryResult.IsValid);
        archiveResult.Diagnostics.Count.Should().Be(directoryResult.Diagnostics.Count);
    }

    private sealed class TempFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bcpvt-arc-" + System.IO.Path.GetRandomFileName() + ".tgz");

        public TempFile(byte[] content) => File.WriteAllBytes(Path, content);

        public void Dispose()
        {
            try { File.Delete(Path); } catch { /* best-effort */ }
        }
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bcpvt-arc-" + System.IO.Path.GetRandomFileName());

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best-effort */ }
        }
    }
}
