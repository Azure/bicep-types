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

    // ── Phase-6 archive behaviour ────────────────────────────────────────────

    [TestMethod]
    public void Archive_file_input_missing_file_returns_package_path_invalid()
    {
        var result = Validator.Validate(TypePackageValidationInput.ForArchiveFile("some/missing-types.tgz"));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.PackagePathInvalid);
        result.Summary.ErrorCount.Should().Be(1);
    }

    [TestMethod]
    public void Archive_stream_input_non_gzip_bytes_reports_archive_invalid()
    {
        using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.ArchivePackageInvalid);
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

    // ── Phase-7 format version awareness ─────────────────────────────────────

    [TestMethod]
    public void Unsupported_format_version_returns_single_unsupported_error()
    {
        using var pkg = CreateMinimalPackage();
        var options = new TypePackageValidationOptions { FormatVersion = (TypePackageFormatVersion)999 };

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path), options);

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnsupportedFormatVersion);
    }

    [TestMethod]
    public void Explicit_bicep_types_v1_validates_valid_package()
    {
        using var pkg = CreateMinimalPackage();
        var options = new TypePackageValidationOptions { FormatVersion = TypePackageFormatVersion.BicepTypesV1 };

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path), options);

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Explicit_bicep_types_v1_compatible_reader_accepts_legacy_form_with_warning()
    {
        // Explicit BicepTypesV1 + CompatibleReader + a legacy scope form: version plumbing must not
        // alter existing compatible-reader policy (single BCPVT023 warning, still valid).
        using var pkg = CreatePackageWithContent(
            "{\"resources\":{\"My.Rp/x@2026-01-01\":{\"$ref\":\"types.json#/0\"}}," +
            "\"resourceFunctions\":{},\"namespaceFunctions\":[]}",
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"scopeType\":0}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}]");
        var options = new TypePackageValidationOptions
        {
            FormatVersion = TypePackageFormatVersion.BicepTypesV1,
            Mode = TypePackageValidationMode.CompatibleReader,
        };

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path), options);

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.CompatibilityFormUsed);
        result.Summary.WarningCount.Should().Be(1);
        result.Summary.ErrorCount.Should().Be(0);
    }

    [TestMethod]
    public void Unsupported_format_version_wins_over_invalid_input_path()
    {
        var options = new TypePackageValidationOptions { FormatVersion = (TypePackageFormatVersion)999 };

        var result = Validator.Validate(
            TypePackageValidationInput.ForDirectory("nonexistent-dir-that-does-not-exist"), options);

        // The version gate runs before input resolution/reading, so the path error never appears.
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnsupportedFormatVersion);
        result.Diagnostics.Should().NotContain(d => d.Code == TypeValidationDiagnosticCodes.PackagePathInvalid);
    }

    [TestMethod]
    public void Unsupported_format_version_does_not_consume_archive_stream()
    {
        using var stream = new TrackingStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var options = new TypePackageValidationOptions { FormatVersion = (TypePackageFormatVersion)999 };

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"), options);

        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.UnsupportedFormatVersion);
        stream.ReadInvoked.Should().BeFalse("the version gate must run before the caller's stream is read");
    }

    [TestMethod]
    public void Unsupported_format_version_is_invalid_and_counted_in_summary()
    {
        using var pkg = CreateMinimalPackage();
        var options = new TypePackageValidationOptions { FormatVersion = (TypePackageFormatVersion)999 };

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path), options);

        result.IsValid.Should().BeFalse();
        result.Summary.ErrorCount.Should().Be(1);
        result.Summary.WarningCount.Should().Be(0);
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

    /// <summary>
    /// Non-seekable read-only stream that records whether it was ever read, used to prove the
    /// format-version gate returns before the caller's archive stream is consumed.
    /// </summary>
    private sealed class TrackingStream : Stream
    {
        private readonly MemoryStream inner;

        public TrackingStream(byte[] content) => inner = new MemoryStream(content);

        public bool ReadInvoked { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadInvoked = true;
            return inner.Read(buffer, offset, count);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) { inner.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

