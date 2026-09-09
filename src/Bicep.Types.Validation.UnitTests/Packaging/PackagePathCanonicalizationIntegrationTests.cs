// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text.Json;
using Azure.Bicep.Types.Validation.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class PackagePathCanonicalizationIntegrationTests
{
    private const string TypesJson = @"[
  { ""$type"": ""ObjectType"", ""name"": ""body"", ""properties"": {} },
  {
    ""$type"": ""ResourceType"",
    ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""#/0"" },
    ""readableScopes"": 8,
    ""writableScopes"": 8
  }
]";

    private static readonly TypePackageValidator Validator = new();

    [TestMethod]
    [DataRow("./types.json#/1", "types.json")]
    [DataRow(".//types.json#/1", "types.json")]
    [DataRow("common/./types.json#/1", "common/types.json")]
    [DataRow("common////types.json#/1", "common/types.json")]
    [DataRow(@"common\types.json#/1", "common/types.json")]
    public void Safe_aliases_have_matching_directory_and_archive_results(
        string reference,
        string canonicalTargetPath)
    {
        string indexJson = ResourceIndex(reference);
        using var directory = new TempDir();
        WritePackageFile(directory.Path, "index.json", indexJson);
        WritePackageFile(directory.Path, canonicalTargetPath, TypesJson);

        var archive = TarGzTestArchive.FromTextFiles(
            ("index.json", indexJson),
            (canonicalTargetPath, TypesJson));
        using var archiveStream = new MemoryStream(archive);
        var options = new TypePackageValidationOptions { ValidateUnreachableFiles = true };

        var directoryResult = Validator.Validate(
            TypePackageValidationInput.ForDirectory(directory.Path),
            options);
        var archiveResult = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(archiveStream, "types.tgz"),
            options);

        directoryResult.IsValid.Should().BeTrue();
        directoryResult.Diagnostics.Should().BeEmpty();
        archiveResult.IsValid.Should().BeTrue();
        archiveResult.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Multiple_aliases_do_not_duplicate_file_validation()
    {
        const string indexJson = @"{
  ""resources"": {
    ""My.Rp/a@2026-01-01"": { ""$ref"": ""types.json#/1"" },
    ""My.Rp/b@2026-01-01"": { ""$ref"": ""./types.json#/2"" }
  },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";
        const string typesJson = @"[
  { ""$type"": ""ObjectType"", ""name"": ""body"", ""properties"": {} },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/a@2026-01-01"", ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 },
  { ""$type"": ""ResourceType"", ""name"": ""My.Rp/b@2026-01-01"", ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 },
  { ""$type"": ""IntegerType"", ""minValue"": 10, ""maxValue"": 5 }
]";
        using var directory = new TempDir();
        WritePackageFile(directory.Path, "index.json", indexJson);
        WritePackageFile(directory.Path, "types.json", typesJson);

        var result = Validator.Validate(
            TypePackageValidationInput.ForDirectory(directory.Path),
            new TypePackageValidationOptions { ValidateUnreachableFiles = true });

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.NumericRangeInvalid);
    }

    [TestMethod]
    [DataRow("../types.json#/0")]
    [DataRow("common/../types.json#/0")]
    [DataRow("/types.json#/0")]
    [DataRow("C:/types.json#/0")]
    [DataRow("C:types.json#/0")]
    [DataRow("./C:/types.json#/0")]
    [DataRow(".//C:types.json#/0")]
    [DataRow(@"\\server\share\types.json#/0")]
    [DataRow(".#/0")]
    [DataRow("types.json/#/0")]
    public void Invalid_reference_path_reports_only_reference_syntax_diagnostic(string reference)
    {
        using var directory = new TempDir();
        WritePackageFile(directory.Path, "index.json", ResourceIndex(reference));

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(directory.Path));

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
    }

    [TestMethod]
    public void Cycle_using_path_alias_terminates_without_duplicate_diagnostics()
    {
        const string typesJson = @"[
  {
    ""$type"": ""ObjectType"",
    ""name"": ""body"",
    ""properties"": {
      ""self"": { ""type"": { ""$ref"": ""./types.json#/0"" }, ""flags"": 0 }
    }
  },
  {
    ""$type"": ""ResourceType"",
    ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""types.json#/0"" },
    ""readableScopes"": 8,
    ""writableScopes"": 8
  }
]";
        using var directory = new TempDir();
        WritePackageFile(directory.Path, "index.json", ResourceIndex("types.json#/1"));
        WritePackageFile(directory.Path, "types.json", typesJson);

        var result = Validator.Validate(
            TypePackageValidationInput.ForDirectory(directory.Path),
            new TypePackageValidationOptions { ValidateUnreachableFiles = true });

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    private static string ResourceIndex(string reference) => $@"{{
  ""resources"": {{
    ""My.Rp/x@2026-01-01"": {{ ""$ref"": {JsonSerializer.Serialize(reference)} }}
  }},
  ""resourceFunctions"": {{}},
  ""namespaceFunctions"": []
}}";

    private static void WritePackageFile(string root, string packageRelativePath, string content)
    {
        string physicalPath = Path.Combine(root, packageRelativePath.Replace('/', Path.DirectorySeparatorChar));
        string? directory = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(physicalPath, content);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "bcpvt-path-" + System.IO.Path.GetRandomFileName());

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
            }
        }
    }
}