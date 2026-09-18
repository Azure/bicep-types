// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Hygiene;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.UnitTests.Graph;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Hygiene;

[TestClass]
public class PackageHygieneTests
{
    private const string MinimalIndexJson = @"{
  ""resources"": {},
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";

    private static readonly TypePackageValidator Validator = new();

    [TestMethod]
    public void Default_validation_ignores_unreachable_type_file()
    {
        using var dir = new TempDir();
        WriteIndex(dir);
        File.WriteAllText(Path.Combine(dir.Path, "orphan.json"), "[]");

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(dir.Path));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void ValidateUnreachableFiles_reports_unreachable_type_file()
    {
        using var dir = new TempDir();
        WriteIndex(dir);
        File.WriteAllText(Path.Combine(dir.Path, "orphan.json"), "[]");

        var result = Validate(dir, validateUnreachable: true);

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnreachablePackageFile);
    }

    [TestMethod]
    public void ValidateUnreachableFiles_reports_unexpected_non_json_file()
    {
        using var dir = new TempDir();
        WriteIndex(dir);
        File.WriteAllText(Path.Combine(dir.Path, "README.md"), "notes");

        var result = Validate(dir, validateUnreachable: true);

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnexpectedPackageFile);
    }

    [TestMethod]
    public void ValidateUnreachableFiles_validates_malformed_unreachable_type_file()
    {
        using var dir = new TempDir();
        WriteIndex(dir);
        File.WriteAllText(Path.Combine(dir.Path, "orphan.json"), "this is not json");

        var result = Validate(dir, validateUnreachable: true);

        result.Diagnostics.Select(d => d.Code).Should().Contain(TypeValidationDiagnosticCodes.UnreachablePackageFile);
        result.Diagnostics.Select(d => d.Code).Should().Contain(TypeValidationDiagnosticCodes.JsonSyntaxInvalid);
    }

    [TestMethod]
    public void ValidateUnreachableFiles_validates_semantic_defects_in_unreachable_type_file()
    {
        using var dir = new TempDir();
        WriteIndex(dir);
        File.WriteAllText(Path.Combine(dir.Path, "orphan.json"),
            @"[ { ""$type"": ""IntegerType"", ""minValue"": 10, ""maxValue"": 5 } ]");

        var result = Validate(dir, validateUnreachable: true);

        result.Diagnostics.Select(d => d.Code).Should().Contain(TypeValidationDiagnosticCodes.UnreachablePackageFile);
        result.Diagnostics.Select(d => d.Code).Should().Contain(TypeValidationDiagnosticCodes.NumericRangeInvalid);
    }

    [TestMethod]
    public void ValidateUnreachableFiles_validates_graph_edges_in_unreachable_type_file()
    {
        using var dir = new TempDir();
        WriteIndex(dir);
        File.WriteAllText(Path.Combine(dir.Path, "orphan.json"),
            @"[ { ""$type"": ""ArrayType"", ""itemType"": { ""$ref"": ""#/9"" } } ]");

        var result = Validate(dir, validateUnreachable: true);

        result.Diagnostics.Select(d => d.Code).Should().Contain(TypeValidationDiagnosticCodes.UnreachablePackageFile);
        result.Diagnostics.Select(d => d.Code).Should().Contain(TypeValidationDiagnosticCodes.ReferenceIndexOutOfRange);
    }

    [TestMethod]
    public void ValidateUnreachableFiles_uses_deterministic_file_order()
    {
        using var dir = new TempDir();
        WriteIndex(dir);
        File.WriteAllText(Path.Combine(dir.Path, "b-orphan.json"), "[]");
        File.WriteAllText(Path.Combine(dir.Path, "a-orphan.json"), "[]");

        var result = Validate(dir, validateUnreachable: true);

        var unreachablePaths = result.Diagnostics
            .Where(d => d.Code == TypeValidationDiagnosticCodes.UnreachablePackageFile)
            .Select(d => d.Path)
            .ToList();

        unreachablePaths.Should().Equal("a-orphan.json", "b-orphan.json");
    }

    [TestMethod]
    public void ValidateUnreachableFiles_does_not_duplicate_reachable_file_diagnostics()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "index.json"), @"{
  ""resources"": { ""Sample.Provider/widgets@2026-01-01"": { ""$ref"": ""types.json#/1"" } },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}");
        File.WriteAllText(Path.Combine(dir.Path, "types.json"), @"[
  { ""$type"": ""ObjectType"", ""name"": ""body"", ""properties"": { ""size"": { ""type"": { ""$ref"": ""#/2"" }, ""flags"": 0 } } },
  { ""$type"": ""ResourceType"", ""name"": ""Sample.Provider/widgets@2026-01-01"", ""body"": { ""$ref"": ""#/0"" }, ""readableScopes"": 8, ""writableScopes"": 8 },
  { ""$type"": ""IntegerType"", ""minValue"": 10, ""maxValue"": 5 }
]");
        File.WriteAllText(Path.Combine(dir.Path, "orphan.json"), "[]");

        var result = Validate(dir, validateUnreachable: true);

        // The reachable defect is reported exactly once even with strict hygiene enabled.
        result.Diagnostics.Count(d => d.Code == TypeValidationDiagnosticCodes.NumericRangeInvalid).Should().Be(1);
        result.Diagnostics.Should().Contain(d => d.Code == TypeValidationDiagnosticCodes.UnreachablePackageFile);
    }

    [TestMethod]
    [DataRow("C:orphan.json")]
    [DataRow(@"dir\orphan.json")]
    public void Invalid_enumerated_package_path_is_reported_as_unexpected_without_throwing(
        string enumeratedPath)
    {
        var fileSystem = new RawPathPackageFileSystem(enumeratedPath, "[]");
        var options = new TypePackageValidationOptions { ValidateUnreachableFiles = true };
        var index = GraphTestHelpers.Document("index.json", MinimalIndexJson);
        var provider = new PackageDocumentProvider(fileSystem, index, options);
        IReadOnlyList<TypeValidationDiagnostic>? diagnostics = null;

        Action act = () => diagnostics = PackageHygieneValidator.Validate(
            fileSystem,
            provider,
            options,
            new HashSet<TypeNodeId>());

        act.Should().NotThrow();
        diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnexpectedPackageFile);
    }

    private static TypePackageValidationResult Validate(TempDir dir, bool validateUnreachable)
    {
        var options = new TypePackageValidationOptions { ValidateUnreachableFiles = validateUnreachable };
        return Validator.Validate(TypePackageValidationInput.ForDirectory(dir.Path), options);
    }

    private static void WriteIndex(TempDir dir) =>
        File.WriteAllText(Path.Combine(dir.Path, "index.json"), MinimalIndexJson);

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bcpvt-hyg-" + System.IO.Path.GetRandomFileName());

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best-effort */ }
        }
    }

    private sealed class RawPathPackageFileSystem : IPackageFileSystem
    {
        private readonly string path;
        private readonly byte[] content;

        public RawPathPackageFileSystem(string path, string content)
        {
            this.path = path;
            this.content = Encoding.UTF8.GetBytes(content);
        }

        public bool FileExists(string packageRelativePath) =>
            string.Equals(packageRelativePath, path, StringComparison.Ordinal);

        public bool TryReadAllBytes(string packageRelativePath, out byte[] bytes, out string error)
        {
            if (FileExists(packageRelativePath))
            {
                bytes = content;
                error = string.Empty;
                return true;
            }

            bytes = Array.Empty<byte>();
            error = "File not found.";
            return false;
        }

        public IEnumerable<string> EnumerateFiles()
        {
            yield return path;
        }
    }
}
