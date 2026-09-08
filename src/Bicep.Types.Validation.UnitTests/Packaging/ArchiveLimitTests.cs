// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class ArchiveLimitTests
{
    private const string MinimalIndexJson = @"{
  ""resources"": {},
  ""resourceFunctions"": {},
  ""namespaceFunctions"": []
}";

    private static readonly TypePackageValidator Validator = new();

    [TestMethod]
    public void Archive_stream_at_compressed_byte_limit_succeeds()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxCompressedArchiveBytes: archive.Length));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Archive_stream_over_compressed_byte_limit_fails_and_remains_open()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxCompressedArchiveBytes: archive.Length - 1L));

        AssertArchiveFailure(result, "compressed archive");
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public void Archive_stream_counts_trailing_input_against_compressed_byte_limit()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        var archiveWithTrailingByte = archive.Concat(new byte[] { 0 }).ToArray();
        using var stream = new MemoryStream(archiveWithTrailingByte);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxCompressedArchiveBytes: archive.Length));

        AssertArchiveFailure(result, "compressed archive");
    }

    [TestMethod]
    public void Archive_file_over_compressed_byte_limit_fails_before_expansion()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var file = new TempArchiveFile(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveFile(file.Path),
            Options(maxCompressedArchiveBytes: archive.Length - 1L));

        AssertArchiveFailure(result, "compressed archive");
    }

    [TestMethod]
    public void Archive_at_expanded_byte_limit_succeeds()
    {
        var tar = TarGzTestArchive.BuildTar(new[] { TarGzTestEntry.File("index.json", MinimalIndexJson) });
        var archive = TarGzTestArchive.GzipCompress(tar);
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(
                maxCompressedArchiveBytes: archive.Length,
                maxExpandedArchiveBytes: tar.Length));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Archive_over_expanded_byte_limit_fails_during_decompression()
    {
        var tar = TarGzTestArchive.BuildTar(new[] { TarGzTestEntry.File("index.json", MinimalIndexJson) });
        var archive = TarGzTestArchive.GzipCompress(tar);
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(
                maxCompressedArchiveBytes: archive.Length,
                maxExpandedArchiveBytes: tar.Length - 1L));

        AssertArchiveFailure(result, "expanded archive");
    }

    [TestMethod]
    public void Package_file_at_byte_limit_succeeds()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxPackageFileBytes: Encoding.UTF8.GetByteCount(MinimalIndexJson)));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Package_file_over_byte_limit_fails_before_payload_is_retained()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxPackageFileBytes: Encoding.UTF8.GetByteCount(MinimalIndexJson) - 1L));

        AssertArchiveFailure(result, "package file 'index.json'");
    }

    [TestMethod]
    public void Archive_at_package_file_count_limit_succeeds()
    {
        var archive = TarGzTestArchive.FromTextFiles(
            ("index.json", MinimalIndexJson),
            ("unused.json", "[]"));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxPackageFileCount: 2));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void Archive_over_package_file_count_limit_fails()
    {
        var archive = TarGzTestArchive.FromTextFiles(
            ("index.json", MinimalIndexJson),
            ("unused.json", "[]"));
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxPackageFileCount: 1));

        AssertArchiveFailure(result, "more than 1 package files");
    }

    [TestMethod]
    public void Oversized_pax_metadata_fails_without_becoming_a_package_file()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            new TarGzTestEntry("PaxHeaders.X/entry", new byte[17], (byte)'x'),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxPackageFileBytes: 16));

        AssertArchiveFailure(result, "tar metadata entry");
    }

    [TestMethod]
    public void Pax_size_override_over_package_file_limit_fails_before_file_allocation()
    {
        var archive = TarGzTestArchive.Build(new[]
        {
            new TarGzTestEntry("PaxHeaders.X/types.json", Encoding.ASCII.GetBytes("11 size=65\n"), (byte)'x'),
            TarGzTestEntry.File("types.json", string.Empty),
        });
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxPackageFileBytes: 64));

        AssertArchiveFailure(result, "package file 'types.json'");
    }

    [TestMethod]
    public void Excessive_non_file_tar_entries_fail_at_internal_archive_entry_limit()
    {
        var entries = new List<TarGzTestEntry>();
        for (int index = 0; index < 37; index++)
        {
            entries.Add(TarGzTestEntry.Directory($"directory-{index}"));
        }

        var archive = TarGzTestArchive.Build(entries);
        using var stream = new MemoryStream(archive);

        var result = Validator.Validate(
            TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"),
            Options(maxPackageFileCount: 1));

        AssertArchiveFailure(result, "more than 36 tar entries");
    }

    [TestMethod]
    public void Non_seekable_partial_read_archive_stream_succeeds_and_remains_open()
    {
        var archive = TarGzTestArchive.FromTextFiles(("index.json", MinimalIndexJson));
        using var stream = new ChunkedNonSeekableReadStream(archive, maximumReadSize: 7);

        var result = Validator.Validate(TypePackageValidationInput.ForArchiveStream(stream, "types.tgz"));

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public void Gnu_long_name_is_applied_to_the_following_package_file()
    {
        string longName = "deeply/nested/" + new string('a', 120) + "/types.json";
        var archive = TarGzTestArchive.Build(new[]
        {
            new TarGzTestEntry("././@LongLink", Encoding.ASCII.GetBytes(longName + "\0"), (byte)'L'),
            TarGzTestEntry.File("placeholder.json", "[]"),
        });

        var result = TarGzArchiveReader.Read(archive);

        result.Success.Should().BeTrue();
        result.Entries.Should().ContainSingle()
            .Which.RawName.Should().Be(longName);
    }

    private static TypePackageValidationOptions Options(
        long maxCompressedArchiveBytes = 64L * 1024L * 1024L,
        long maxExpandedArchiveBytes = 256L * 1024L * 1024L,
        long maxPackageFileBytes = 32L * 1024L * 1024L,
        int maxPackageFileCount = 4096) =>
        new TypePackageValidationOptions
        {
            ArchiveLimits = new TypePackageArchiveLimits(
                maxCompressedArchiveBytes,
                maxExpandedArchiveBytes,
                maxPackageFileBytes,
                maxPackageFileCount),
        };

    private static void AssertArchiveFailure(TypePackageValidationResult result, string expectedMessageFragment)
    {
        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Should().Match<TypeValidationDiagnostic>(diagnostic =>
                diagnostic.Code == TypeValidationDiagnosticCodes.ArchivePackageInvalid &&
                diagnostic.Message.Contains(expectedMessageFragment, StringComparison.Ordinal));
    }

    private sealed class TempArchiveFile : IDisposable
    {
        public TempArchiveFile(byte[] content)
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "bcpvt-limit-" + System.IO.Path.GetRandomFileName() + ".tgz");
            File.WriteAllBytes(Path, content);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                File.Delete(Path);
            }
            catch
            {
            }
        }
    }

    private sealed class ChunkedNonSeekableReadStream : Stream
    {
        private readonly MemoryStream inner;
        private readonly int maximumReadSize;
        private bool disposed;

        public ChunkedNonSeekableReadStream(byte[] content, int maximumReadSize)
        {
            inner = new MemoryStream(content, writable: false);
            this.maximumReadSize = maximumReadSize;
        }

        public override bool CanRead => !disposed;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, Math.Min(count, maximumReadSize));

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            disposed = true;
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}