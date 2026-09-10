// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Azure.Bicep.Types.Validation.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class ReferencePlacementIntegrationTests
{
    private const string IndexToTypes = @"{
  ""resources"": {
    ""My.Rp/x@2026-01-01"": { ""$ref"": ""types.json#/1"" }
  },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";

    private const string TypesWithMissingCrossFileBody = @"[
  { ""$type"": ""ObjectType"", ""name"": ""body"", ""properties"": {} },
  {
    ""$type"": ""ResourceType"",
    ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""missing.json#/0"" },
    ""readableScopes"": 8,
    ""writableScopes"": 8
  }
]";

    private const string TypesWithExistingCrossFileBody = @"[
  { ""$type"": ""ObjectType"", ""name"": ""body"", ""properties"": {} },
  {
    ""$type"": ""ResourceType"",
    ""name"": ""My.Rp/x@2026-01-01"",
    ""body"": { ""$ref"": ""other.json#/0"" },
    ""readableScopes"": 8,
    ""writableScopes"": 8
  }
]";

    private static readonly TypePackageValidator Validator = new();

    [TestMethod]
    [DataRow(TypePackageValidationMode.CanonicalWriter)]
    [DataRow(TypePackageValidationMode.CompatibleReader)]
    public void Missing_cross_file_target_from_type_file_reports_only_placement_error(
        TypePackageValidationMode mode)
    {
        using var package = Package(IndexToTypes, TypesWithMissingCrossFileBody);

        var result = Validator.Validate(
            TypePackageValidationInput.ForDirectory(package.Path),
            new TypePackageValidationOptions { Mode = mode });

        AssertSinglePlacementDiagnostic(result, "types.json", "/1/body/$ref", "missing.json#/0");
        result.Diagnostics.Should().NotContain(d => d.Code == TypeValidationDiagnosticCodes.ReferencedTypeFileMissing);
    }

    [TestMethod]
    public void Existing_cross_file_target_from_type_file_is_not_loaded()
    {
        using var package = Package(IndexToTypes, TypesWithExistingCrossFileBody, "this is not json");

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(package.Path));

        AssertSinglePlacementDiagnostic(result, "types.json", "/1/body/$ref", "other.json#/0");
        result.Diagnostics.Should().NotContain(d => d.Code == TypeValidationDiagnosticCodes.JsonSyntaxInvalid);
    }

    [TestMethod]
    [DataRow(TypePackageValidationMode.CanonicalWriter)]
    [DataRow(TypePackageValidationMode.CompatibleReader)]
    public void Same_file_reference_in_index_reports_only_placement_error(TypePackageValidationMode mode)
    {
        const string indexJson = @"{
  ""resources"": {
    ""My.Rp/x@2026-01-01"": { ""$ref"": ""#/0"" }
  },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";
        using var package = Package(indexJson, typesJson: null);

        var result = Validator.Validate(
            TypePackageValidationInput.ForDirectory(package.Path),
            new TypePackageValidationOptions { Mode = mode });

        AssertSinglePlacementDiagnostic(
            result,
            "index.json",
            "/resources/My.Rp~1x@2026-01-01/$ref",
            "#/0");
    }

    [TestMethod]
    public void Directory_and_archive_report_the_same_placement_diagnostic()
    {
        using var package = Package(IndexToTypes, TypesWithMissingCrossFileBody);
        var archive = TarGzTestArchive.FromTextFiles(
            ("index.json", IndexToTypes),
            ("types.json", TypesWithMissingCrossFileBody));
        using var archiveStream = new MemoryStream(archive);

        var directoryResult = Validator.Validate(TypePackageValidationInput.ForDirectory(package.Path));
        var archiveResult = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(archiveStream, "types.tgz"));

        var directoryDiagnostic = directoryResult.Diagnostics.Should().ContainSingle().Subject;
        var archiveDiagnostic = archiveResult.Diagnostics.Should().ContainSingle().Subject;
        archiveDiagnostic.Code.Should().Be(directoryDiagnostic.Code);
        archiveDiagnostic.Path.Should().Be(directoryDiagnostic.Path);
        archiveDiagnostic.JsonPointer.Should().Be(directoryDiagnostic.JsonPointer);
        archiveDiagnostic.Message.Should().Be(directoryDiagnostic.Message);
    }

    [TestMethod]
    public void Strict_hygiene_reports_existing_invalid_target_as_unreachable()
    {
        using var package = Package(IndexToTypes, TypesWithExistingCrossFileBody, "[]");

        var result = Validator.Validate(
            TypePackageValidationInput.ForDirectory(package.Path),
            new TypePackageValidationOptions { ValidateUnreachableFiles = true });

        result.Diagnostics.Select(d => d.Code).Should().BeEquivalentTo(
            TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid,
            TypeValidationDiagnosticCodes.UnreachablePackageFile);
        result.Diagnostics.Should().Contain(d =>
            d.Code == TypeValidationDiagnosticCodes.UnreachablePackageFile && d.Path == "other.json");
        result.Diagnostics.Should().NotContain(d => d.Code == TypeValidationDiagnosticCodes.ReferencedTypeFileMissing);
    }

    private static void AssertSinglePlacementDiagnostic(
        TypePackageValidationResult result,
        string expectedPath,
        string expectedPointer,
        string rawReference)
    {
        result.IsValid.Should().BeFalse();
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be(TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid);
        diagnostic.Path.Should().Be(expectedPath);
        diagnostic.JsonPointer.Should().Be(expectedPointer);
        diagnostic.Line.Should().NotBeNull();
        diagnostic.Column.Should().NotBeNull();
        diagnostic.Message.Should().Contain(rawReference);
    }

    private static TempDir Package(string indexJson, string? typesJson, string? otherJson = null)
    {
        var package = new TempDir();
        File.WriteAllText(Path.Combine(package.Path, "index.json"), indexJson);
        if (typesJson != null)
        {
            File.WriteAllText(Path.Combine(package.Path, "types.json"), typesJson);
        }
        if (otherJson != null)
        {
            File.WriteAllText(Path.Combine(package.Path, "other.json"), otherJson);
        }
        return package;
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "bcpvt-placement-" + System.IO.Path.GetRandomFileName());

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