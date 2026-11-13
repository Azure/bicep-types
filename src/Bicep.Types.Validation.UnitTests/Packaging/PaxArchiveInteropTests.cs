// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

/// <summary>
/// Interop coverage proving that gzip-compressed tar archives produced the way Bicep's own tgz writer
/// produces them — via <see cref="TarWriter"/> in <see cref="TarEntryFormat.Pax"/> using
/// <see cref="PaxTarEntry"/> — validate successfully.  .NET emits a PAX extended-header entry (typeflag
/// <c>'x'</c>) before every file entry; earlier the reader forwarded those headers as unsupported members
/// and rejected real Bicep archives.  These tests use the framework's own tar writer (available on
/// <c>net8.0</c>) rather than the hand-rolled ustar helper so a regression is caught against the real
/// producer format.
/// </summary>
[TestClass]
public class PaxArchiveInteropTests
{
    private const string MinimalIndexJson = @"{
  ""resources"": {},
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";

    private const string ResourceIndexJson = @"{
  ""resources"": {
    ""Sample.Provider/widgets@2026-01-01"": {
      ""$ref"": ""types.json#/2""
    }
  },
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";

    private const string ResourceTypesJson = @"[
  { ""$type"": ""StringType"" },
  {
    ""$type"": ""ObjectType"",
    ""name"": ""widgetBody"",
    ""properties"": {
      ""name"": {
        ""type"": { ""$ref"": ""#/0"" },
        ""flags"": 1,
        ""description"": ""The widget name.""
      }
    }
  },
  {
    ""$type"": ""ResourceType"",
    ""name"": ""Sample.Provider/widgets@2026-01-01"",
    ""body"": { ""$ref"": ""#/1"" },
    ""readableScopes"": 8,
    ""writableScopes"": 8
  }
]";

    private static readonly TypePackageValidator Validator = new();

    [TestMethod]
    public void Pax_archive_stream_validates_minimal_package()
    {
        var archive = BuildPaxArchive(("index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Pax_archive_stream_validates_multi_file_resource_package()
    {
        var archive = BuildPaxArchive(
            ("index.json", ResourceIndexJson),
            ("types.json", ResourceTypesJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Pax_archive_does_not_report_extended_header_as_unsupported_member()
    {
        var archive = BuildPaxArchive(("index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.Diagnostics.Should().NotContain(d =>
            d.Code == TypeValidationDiagnosticCodes.ArchivePackageInvalid ||
            d.Code == TypeValidationDiagnosticCodes.ArchiveMemberPathInvalid);
    }

    [TestMethod]
    public void Pax_archive_reader_consumes_extended_headers_and_returns_only_file_members()
    {
        var archive = BuildPaxArchive(
            ("index.json", MinimalIndexJson),
            ("types.json", "[]"));

        var readResult = TarGzArchiveReader.Read(archive);

        readResult.Success.Should().BeTrue();
        readResult.Entries.Should().OnlyContain(e => e.TypeFlag == (byte)'0' || e.TypeFlag == 0);
        readResult.Entries.Select(e => e.RawName).Should().BeEquivalentTo(new[] { "index.json", "types.json" });
    }

    [TestMethod]
    public void Pax_archive_with_long_member_name_uses_the_extended_path()
    {
        // Names longer than the 100-byte ustar name field force .NET to emit the real path only in the
        // PAX "path" extended attribute, exercising the reader's override handling.
        var longName = "deeply/nested/" + new string('a', 120) + "/types.json";
        var archive = BuildPaxArchive(
            ("index.json", MinimalIndexJson),
            (longName, "[]"));

        var readResult = TarGzArchiveReader.Read(archive);

        readResult.Success.Should().BeTrue();
        readResult.Entries.Select(e => e.RawName).Should().Contain(longName);
    }

    private static byte[] BuildPaxArchive(params (string Name, string Text)[] files)
    {
        using var outer = new MemoryStream();
        using (var gzip = new GZipStream(outer, CompressionLevel.Optimal, leaveOpen: true))
        using (var tar = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: true))
        {
            foreach (var (name, text) in files)
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes(text)),
                };
                tar.WriteEntry(entry);
            }
        }

        return outer.ToArray();
    }
}
