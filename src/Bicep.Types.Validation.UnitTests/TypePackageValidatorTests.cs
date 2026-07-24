// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Azure.Bicep.Types.Validation.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests;

[TestClass]
public class TypePackageValidatorTests
{
    private static readonly TypePackageValidator Validator = new();

    // ── Phase-2 real-file tests ──────────────────────────────────────────────

    [TestMethod]
    public void Valid_package_directory_returns_valid_result()
    {
        using var pkg = CreateMinimalPackage();
        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
        result.Summary.ErrorCount.Should().Be(0);
    }

    [TestMethod]
    public void Valid_index_file_input_returns_valid_result()
    {
        using var pkg = CreateMinimalPackage();
        var indexPath = Path.Combine(pkg.Path, "index.json");
        var result = Validator.Validate(TypePackageValidationInput.ForIndexFile(indexPath));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Nonexistent_directory_returns_package_path_invalid()
    {
        var result = Validator.Validate(TypePackageValidationInput.ForDirectory("nonexistent-dir-that-does-not-exist"));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.PackagePathInvalid);
    }

    [TestMethod]
    public void Directory_with_missing_index_json_returns_index_file_missing()
    {
        using var emptyDir = new TempDir();
        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(emptyDir.Path));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.IndexFileMissing);
    }

    [TestMethod]
    public void Malformed_index_json_returns_invalid_and_stops_before_type_file_validation()
    {
        using var pkg = CreatePackageWithContent("{ not valid json }", null);
        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.JsonSyntaxInvalid);
    }

    [TestMethod]
    public void Malformed_type_file_returns_invalid_without_hiding_index_errors()
    {
        // index.json references types.json; types.json has a syntax error
        const string indexJson = @"{
  ""resources"": { ""Sample/res@2026-01-01"": { ""$ref"": ""types.json#/0"" } },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";
        using var pkg = CreatePackageWithContent(indexJson, "this is not json");
        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.JsonSyntaxInvalid)
            .Which.Path.Should().Be("types.json");
    }

    // ── Phase-1 behaviour preserved ──────────────────────────────────────────

    [TestMethod]
    public void Archive_file_input_returns_not_implemented_error()
    {
        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("some/types.tgz"));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveValidationNotImplemented);
        result.Summary.ErrorCount.Should().Be(1);
    }

    [TestMethod]
    public void Archive_stream_input_returns_not_implemented_error()
    {
        using var stream = new MemoryStream();

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchiveValidationNotImplemented);
    }

    [TestMethod]
    public void Result_echoes_selected_mode()
    {
        var options = new TypePackageValidationOptions { Mode = TypePackageValidationMode.CompatibleReader };

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("types.tgz"), options);

        result.Mode.Should().Be(TypePackageValidationMode.CompatibleReader);
    }

    [TestMethod]
    public void Default_mode_is_canonical_writer()
    {
        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("types.tgz"));

        result.Mode.Should().Be(TypePackageValidationMode.CanonicalWriter);
    }

    [TestMethod]
    public void Null_input_throws()
    {
        Action act = () => Validator.Validate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Null_max_diagnostics_leaves_truncation_false_for_archive_error()
    {
        var options = new TypePackageValidationOptions { MaxDiagnostics = null };

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("types.tgz"), options);

        result.DiagnosticsTruncated.Should().BeFalse();
    }

    [TestMethod]
    public void Positive_max_diagnostics_not_exceeded_leaves_truncation_false()
    {
        var options = new TypePackageValidationOptions { MaxDiagnostics = 1 };

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("types.tgz"), options);

        result.Diagnostics.Should().ContainSingle();
        result.DiagnosticsTruncated.Should().BeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a temp directory containing a minimal structurally-valid package:
    /// <c>index.json</c> with empty resources/resourceFunctions/namespaceFunctions
    /// and <c>types.json</c> as an empty array.
    /// </summary>
    private static TempDir CreateMinimalPackage()
    {
        var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "index.json"), @"{
  ""resources"": {},
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}");
        return dir;
    }

    /// <summary>
    /// Creates a temp directory with custom <c>index.json</c> content and optionally a
    /// <c>types.json</c> file.
    /// </summary>
    private static TempDir CreatePackageWithContent(string indexJson, string? typesJson)
    {
        var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "index.json"), indexJson);
        if (typesJson != null)
        {
            File.WriteAllText(Path.Combine(dir.Path, "types.json"), typesJson);
        }
        return dir;
    }

    /// <summary>Disposable temp directory that deletes itself on dispose.</summary>
    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bcpvt-test-" + System.IO.Path.GetRandomFileName());

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best-effort */ }
        }
    }
}

