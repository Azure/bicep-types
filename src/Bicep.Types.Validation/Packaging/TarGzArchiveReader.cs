// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// One raw entry read from a tar archive, before package-path validation.
    /// </summary>
    internal readonly struct TarArchiveEntry
    {
        public TarArchiveEntry(string rawName, byte typeFlag, byte[] content)
        {
            RawName = rawName;
            TypeFlag = typeFlag;
            Content = content;
        }

        /// <summary>The raw member name as stored in the tar header (prefix already applied).</summary>
        public string RawName { get; }

        /// <summary>The tar typeflag byte (<c>0</c>/<c>'0'</c> = regular file, <c>'5'</c> = directory, etc.).</summary>
        public byte TypeFlag { get; }

        /// <summary>The member content bytes for regular files; empty for non-file entries.</summary>
        public byte[] Content { get; }
    }

    /// <summary>
    /// Outcome of reading a gzip-compressed tar archive.
    /// </summary>
    internal sealed class TarGzArchiveReadResult
    {
        private TarGzArchiveReadResult(bool success, string? errorMessage, IReadOnlyList<TarArchiveEntry> entries)
        {
            Success = success;
            ErrorMessage = errorMessage;
            Entries = entries;
        }

        /// <summary><c>true</c> when the container decompressed and parsed as a structurally valid tar.</summary>
        public bool Success { get; }

        /// <summary>Reader message describing a fatal container failure; otherwise <c>null</c>.</summary>
        public string? ErrorMessage { get; }

        /// <summary>The raw entries read from the archive; empty on failure.</summary>
        public IReadOnlyList<TarArchiveEntry> Entries { get; }

        public static TarGzArchiveReadResult Failure(string message) =>
            new TarGzArchiveReadResult(false, message, Array.Empty<TarArchiveEntry>());

        public static TarGzArchiveReadResult Ok(IReadOnlyList<TarArchiveEntry> entries) =>
            new TarGzArchiveReadResult(true, null, entries);
    }

    /// <summary>
    /// Minimal read-only reader for gzip-compressed ustar archives (<c>types.tgz</c>).
    /// </summary>
    /// <remarks>
    /// The reader intentionally supports only what a Bicep type package needs: it decompresses the
    /// gzip container and parses ustar 512-byte headers well enough to read regular-file content and
    /// classify directory, symlink, hardlink, and other unsupported entry types.  PAX extended headers
    /// and GNU long-name entries (emitted by .NET's <c>TarWriter</c> in PAX format, which Bicep's own
    /// tgz writer uses) are consumed as metadata rather than treated as members, applying their
    /// <c>path</c> and <c>size</c> overrides to the following file entry. Structural failures are
    /// reported as a single fatal message rather than thrown, so callers can surface a <c>BCPVT029</c>
    /// diagnostic. It does not depend on <c>System.Formats.Tar</c>, which is not available on
    /// <c>netstandard2.0</c>.
    /// </remarks>
    internal static class TarGzArchiveReader
    {
        private const int BlockSize = 512;
        private const int DiscardBufferSize = 8192;
        private const long MaximumMetadataPayloadBytes = 1024L * 1024L;
        private const int NameOffset = 0;
        private const int NameLength = 100;
        private const int SizeOffset = 124;
        private const int SizeLength = 12;
        private const int TypeFlagOffset = 156;
        private const int MagicOffset = 257;
        private const int PrefixOffset = 345;
        private const int PrefixLength = 155;

        // Metadata tar entries that carry information about the following file entry rather than
        // package content of their own.
        private const byte PaxExtendedHeaderTypeFlag = (byte)'x';
        private const byte PaxGlobalHeaderTypeFlag = (byte)'g';
        private const byte GnuLongNameTypeFlag = (byte)'L';
        private const byte GnuLongLinkTypeFlag = (byte)'K';
        private const byte DirectoryTypeFlag = (byte)'5';
        private const byte RegularFileTypeFlag = (byte)'0';
        private const byte AlternateRegularFileTypeFlag = 0;

        /// <summary>Reads all entries from gzip-compressed tar bytes using the default resource limits.</summary>
        public static TarGzArchiveReadResult Read(byte[] archiveBytes)
        {
            if (archiveBytes == null) { throw new ArgumentNullException(nameof(archiveBytes)); }

            using var input = new MemoryStream(archiveBytes, writable: false);
            return Read(input, TypePackageArchiveLimits.Default);
        }

        /// <summary>Reads all entries from a gzip-compressed tar stream within the supplied limits.</summary>
        public static TarGzArchiveReadResult Read(Stream archiveStream, TypePackageArchiveLimits limits)
        {
            if (archiveStream == null) { throw new ArgumentNullException(nameof(archiveStream)); }
            if (limits == null) { throw new ArgumentNullException(nameof(limits)); }

            try
            {
                var compressed = new SizeLimitedReadStream(
                    archiveStream,
                    limits.MaxCompressedArchiveBytes,
                    $"the compressed archive exceeds the configured limit of {limits.MaxCompressedArchiveBytes} bytes");

                TarGzArchiveReadResult result;
                using (var gzip = new GZipStream(compressed, CompressionMode.Decompress, leaveOpen: true))
                {
                    var expanded = new SizeLimitedReadStream(
                        gzip,
                        limits.MaxExpandedArchiveBytes,
                        $"the expanded archive exceeds the configured limit of {limits.MaxExpandedArchiveBytes} bytes");

                    result = ParseTar(expanded, limits);
                    if (!result.Success)
                    {
                        return result;
                    }

                    Drain(expanded);
                }

                Drain(compressed);
                return result;
            }
            catch (ArchiveLimitExceededException ex)
            {
                return TarGzArchiveReadResult.Failure(ex.Message);
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is EndOfStreamException)
            {
                return TarGzArchiveReadResult.Failure("the input is not a valid gzip stream");
            }
        }

        private static TarGzArchiveReadResult ParseTar(Stream tar, TypePackageArchiveLimits limits)
        {
            var entries = new List<TarArchiveEntry>();
            var header = new byte[BlockSize];
            var discardBuffer = new byte[DiscardBufferSize];
            long archiveEntryCount = 0;
            long packageFileCount = 0;
            long maximumArchiveEntryCount = ((long)limits.MaxPackageFileCount * 4L) + 32L;
            long maximumMetadataPayloadBytes = Math.Min(limits.MaxPackageFileBytes, MaximumMetadataPayloadBytes);

            // Overrides carried forward from a preceding PAX extended header or GNU long-name entry.
            string? pendingName = null;
            long? pendingSize = null;

            while (true)
            {
                int headerBytesRead = ReadUpTo(tar, header, 0, BlockSize);
                if (headerBytesRead == 0)
                {
                    return TarGzArchiveReadResult.Ok(entries);
                }

                if (headerBytesRead != BlockSize)
                {
                    return TarGzArchiveReadResult.Failure("the tar stream ends in the middle of a header block");
                }

                if (IsZeroBlock(header, 0))
                {
                    // First all-zero block marks the end-of-archive terminator.
                    return TarGzArchiveReadResult.Ok(entries);
                }

                archiveEntryCount++;
                if (archiveEntryCount > maximumArchiveEntryCount)
                {
                    return TarGzArchiveReadResult.Failure(
                        $"the archive contains more than {maximumArchiveEntryCount} tar entries");
                }

                if (!HasUstarMagic(header, 0))
                {
                    return TarGzArchiveReadResult.Failure("a tar header block is missing the 'ustar' magic marker");
                }

                string name = ReadString(header, NameOffset, NameLength);
                string prefix = ReadString(header, PrefixOffset, PrefixLength);
                string rawName = prefix.Length > 0 ? prefix + "/" + name : name;
                byte typeFlag = header[TypeFlagOffset];

                if (!TryReadOctal(header, SizeOffset, SizeLength, out long headerSize) || headerSize < 0)
                {
                    return TarGzArchiveReadResult.Failure("a tar header block has an invalid size field");
                }

                // A metadata entry (PAX header or GNU long name) uses its own header size for its
                // content span. A file/directory entry may have that size overridden by a preceding
                // PAX "size" record (used when the real length does not fit the octal size field).
                bool isMetadata =
                    typeFlag == PaxExtendedHeaderTypeFlag ||
                    typeFlag == PaxGlobalHeaderTypeFlag ||
                    typeFlag == GnuLongNameTypeFlag ||
                    typeFlag == GnuLongLinkTypeFlag;

                long contentSize = isMetadata ? headerSize : (pendingSize ?? headerSize);
                if (contentSize < 0)
                {
                    return TarGzArchiveReadResult.Failure("a tar entry has an invalid content size");
                }

                long paddingSize = (BlockSize - (contentSize % BlockSize)) % BlockSize;

                if (typeFlag == PaxExtendedHeaderTypeFlag || typeFlag == PaxGlobalHeaderTypeFlag)
                {
                    if (contentSize > maximumMetadataPayloadBytes || contentSize > int.MaxValue)
                    {
                        return TarGzArchiveReadResult.Failure(
                            $"a tar metadata entry exceeds the configured internal limit of {maximumMetadataPayloadBytes} bytes");
                    }

                    if (!TryReadPayload(tar, contentSize, out byte[] metadata) ||
                        !SkipExactly(tar, paddingSize, discardBuffer))
                    {
                        return TarGzArchiveReadResult.Failure("a tar metadata entry declares more content than the archive contains");
                    }

                    ParsePaxRecords(metadata, 0, metadata.Length, ref pendingName, ref pendingSize);
                    continue;
                }

                if (typeFlag == GnuLongNameTypeFlag)
                {
                    if (contentSize > maximumMetadataPayloadBytes || contentSize > int.MaxValue)
                    {
                        return TarGzArchiveReadResult.Failure(
                            $"a tar metadata entry exceeds the configured internal limit of {maximumMetadataPayloadBytes} bytes");
                    }

                    if (!TryReadPayload(tar, contentSize, out byte[] longName) ||
                        !SkipExactly(tar, paddingSize, discardBuffer))
                    {
                        return TarGzArchiveReadResult.Failure("a GNU long-name entry declares more content than the archive contains");
                    }

                    pendingName = ReadString(longName, 0, longName.Length);
                    continue;
                }

                if (typeFlag == GnuLongLinkTypeFlag)
                {
                    // Long link targets are irrelevant to package files; consume and ignore.
                    if (contentSize > maximumMetadataPayloadBytes ||
                        !SkipExactly(tar, contentSize, discardBuffer) ||
                        !SkipExactly(tar, paddingSize, discardBuffer))
                    {
                        return TarGzArchiveReadResult.Failure("a GNU long-link entry declares more content than the archive contains");
                    }
                    continue;
                }

                string effectiveName = pendingName ?? rawName;
                pendingName = null;
                pendingSize = null;

                bool isRegularFile = typeFlag == RegularFileTypeFlag || typeFlag == AlternateRegularFileTypeFlag;
                byte[] content;
                if (isRegularFile)
                {
                    packageFileCount++;
                    if (packageFileCount > limits.MaxPackageFileCount)
                    {
                        return TarGzArchiveReadResult.Failure(
                            $"the archive contains more than {limits.MaxPackageFileCount} package files");
                    }

                    if (contentSize > limits.MaxPackageFileBytes)
                    {
                        return TarGzArchiveReadResult.Failure(
                            $"package file '{effectiveName}' exceeds the configured limit of {limits.MaxPackageFileBytes} bytes");
                    }

                    if (contentSize > int.MaxValue)
                    {
                        return TarGzArchiveReadResult.Failure(
                            $"package file '{effectiveName}' exceeds the largest supported in-memory file size");
                    }

                    if (!TryReadPayload(tar, contentSize, out content))
                    {
                        return TarGzArchiveReadResult.Failure("a tar entry declares more content than the archive contains");
                    }
                }
                else
                {
                    content = Array.Empty<byte>();
                    if (!SkipExactly(tar, contentSize, discardBuffer))
                    {
                        return TarGzArchiveReadResult.Failure("a tar entry declares more content than the archive contains");
                    }
                }

                if (!SkipExactly(tar, paddingSize, discardBuffer))
                {
                    return TarGzArchiveReadResult.Failure("a tar entry is missing its expected padding bytes");
                }

                entries.Add(new TarArchiveEntry(effectiveName, typeFlag, content));
            }
        }

        private static bool TryReadPayload(Stream stream, long length, out byte[] payload)
        {
            payload = length == 0 ? Array.Empty<byte>() : new byte[(int)length];
            return ReadUpTo(stream, payload, 0, payload.Length) == payload.Length;
        }

        private static bool SkipExactly(Stream stream, long length, byte[] buffer)
        {
            long remaining = length;
            while (remaining > 0)
            {
                int requested = (int)Math.Min(remaining, buffer.Length);
                int read = stream.Read(buffer, 0, requested);
                if (read == 0)
                {
                    return false;
                }

                remaining -= read;
            }

            return true;
        }

        private static int ReadUpTo(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            return totalRead;
        }

        private static void Drain(Stream stream)
        {
            var buffer = new byte[DiscardBufferSize];
            while (stream.Read(buffer, 0, buffer.Length) > 0)
            {
            }
        }

        /// <summary>
        /// Parses PAX extended-header records (<c>"len key=value\n"</c>) and applies the <c>path</c> and
        /// <c>size</c> overrides to the following file entry.  Unknown keys are ignored.
        /// </summary>
        private static void ParsePaxRecords(byte[] tar, int offset, int length, ref string? pendingName, ref long? pendingSize)
        {
            int i = offset;
            int end = offset + length;

            while (i < end)
            {
                int spaceIndex = i;
                while (spaceIndex < end && tar[spaceIndex] != (byte)' ')
                {
                    spaceIndex++;
                }

                if (spaceIndex >= end || !TryParseDecimal(tar, i, spaceIndex - i, out int recordLength) || recordLength <= 0)
                {
                    return;
                }

                int recordEnd = i + recordLength;
                if (recordEnd > end || recordEnd <= spaceIndex + 1)
                {
                    return;
                }

                int keyStart = spaceIndex + 1;
                int valueEnd = recordEnd - 1; // Exclude the trailing newline.
                int equalsIndex = keyStart;
                while (equalsIndex < valueEnd && tar[equalsIndex] != (byte)'=')
                {
                    equalsIndex++;
                }

                if (equalsIndex < valueEnd)
                {
                    string key = Encoding.ASCII.GetString(tar, keyStart, equalsIndex - keyStart);
                    if (string.Equals(key, "path", StringComparison.Ordinal))
                    {
                        pendingName = Encoding.UTF8.GetString(tar, equalsIndex + 1, valueEnd - (equalsIndex + 1));
                    }
                    else if (string.Equals(key, "size", StringComparison.Ordinal))
                    {
                        string sizeText = Encoding.ASCII.GetString(tar, equalsIndex + 1, valueEnd - (equalsIndex + 1));
                        if (long.TryParse(sizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedSize) && parsedSize >= 0)
                        {
                            pendingSize = parsedSize;
                        }
                    }
                }

                i = recordEnd;
            }
        }

        private static bool TryParseDecimal(byte[] buffer, int offset, int length, out int value)
        {
            value = 0;
            if (length <= 0)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                byte b = buffer[offset + i];
                if (b < (byte)'0' || b > (byte)'9')
                {
                    return false;
                }

                int digit = b - (byte)'0';
                if (value > (int.MaxValue - digit) / 10)
                {
                    return false;
                }

                value = (value * 10) + digit;
            }

            return true;
        }

        private static bool IsZeroBlock(byte[] buffer, int offset)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                if (buffer[offset + i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HasUstarMagic(byte[] buffer, int offset)
        {
            // "ustar" as ASCII bytes; POSIX uses "ustar\0", GNU uses "ustar  ". Match the prefix only.
            return buffer[offset + MagicOffset + 0] == (byte)'u'
                && buffer[offset + MagicOffset + 1] == (byte)'s'
                && buffer[offset + MagicOffset + 2] == (byte)'t'
                && buffer[offset + MagicOffset + 3] == (byte)'a'
                && buffer[offset + MagicOffset + 4] == (byte)'r';
        }

        private static string ReadString(byte[] buffer, int offset, int length)
        {
            int end = offset;
            int limit = offset + length;
            while (end < limit && buffer[end] != 0)
            {
                end++;
            }
            return Encoding.ASCII.GetString(buffer, offset, end - offset);
        }

        private static bool TryReadOctal(byte[] buffer, int offset, int length, out long value)
        {
            value = 0;
            bool sawDigit = false;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[offset + i];
                if (b == 0 || b == (byte)' ')
                {
                    if (sawDigit)
                    {
                        break;
                    }
                    continue;
                }
                if (b < (byte)'0' || b > (byte)'7')
                {
                    return false;
                }
                value = (value << 3) + (b - (byte)'0');
                sawDigit = true;
            }
            return true;
        }

        private sealed class ArchiveLimitExceededException : Exception
        {
            public ArchiveLimitExceededException(string message)
                : base(message)
            {
            }
        }

        private sealed class SizeLimitedReadStream : Stream
        {
            private readonly Stream inner;
            private readonly long limit;
            private readonly string exceededMessage;
            private long bytesRead;

            public SizeLimitedReadStream(Stream inner, long limit, string exceededMessage)
            {
                this.inner = inner;
                this.limit = limit;
                this.exceededMessage = exceededMessage;
            }

            public override bool CanRead => inner.CanRead;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => bytesRead;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (count == 0)
                {
                    return 0;
                }

                long remaining = limit - bytesRead;
                int boundedCount = remaining >= count ? count : (int)remaining + 1;
                int read = inner.Read(buffer, offset, boundedCount);
                bytesRead += read;

                if (bytesRead > limit)
                {
                    throw new ArchiveLimitExceededException(exceededMessage);
                }

                return read;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
