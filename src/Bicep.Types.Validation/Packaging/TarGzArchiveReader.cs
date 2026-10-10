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
    /// <c>path</c> and <c>size</c> overrides to the following file entry.  Structural failures are
    /// reported as a single fatal message rather than thrown, so callers can surface a <c>BCPVT030</c>
    /// diagnostic.  It does not depend on <c>System.Formats.Tar</c>, which is not available on
    /// <c>netstandard2.0</c>.
    /// </remarks>
    internal static class TarGzArchiveReader
    {
        private const int BlockSize = 512;
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

        /// <summary>Reads all entries from a gzip-compressed tar archive held in memory.</summary>
        public static TarGzArchiveReadResult Read(byte[] archiveBytes)
        {
            if (archiveBytes == null) { throw new ArgumentNullException(nameof(archiveBytes)); }

            byte[] tarBytes;
            try
            {
                tarBytes = Decompress(archiveBytes);
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is EndOfStreamException)
            {
                return TarGzArchiveReadResult.Failure("the input is not a valid gzip stream");
            }

            return ParseTar(tarBytes);
        }

        private static byte[] Decompress(byte[] archiveBytes)
        {
            using var input = new MemoryStream(archiveBytes, writable: false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static TarGzArchiveReadResult ParseTar(byte[] tar)
        {
            var entries = new List<TarArchiveEntry>();
            int pos = 0;

            // Overrides carried forward from a preceding PAX extended header or GNU long-name entry.
            string? pendingName = null;
            long? pendingSize = null;

            while (pos + BlockSize <= tar.Length)
            {
                if (IsZeroBlock(tar, pos))
                {
                    // First all-zero block marks the end-of-archive terminator.
                    return TarGzArchiveReadResult.Ok(entries);
                }

                if (!HasUstarMagic(tar, pos))
                {
                    return TarGzArchiveReadResult.Failure("a tar header block is missing the 'ustar' magic marker");
                }

                string name = ReadString(tar, pos + NameOffset, NameLength);
                string prefix = ReadString(tar, pos + PrefixOffset, PrefixLength);
                string rawName = prefix.Length > 0 ? prefix + "/" + name : name;
                byte typeFlag = tar[pos + TypeFlagOffset];

                if (!TryReadOctal(tar, pos + SizeOffset, SizeLength, out long headerSize) || headerSize < 0)
                {
                    return TarGzArchiveReadResult.Failure("a tar header block has an invalid size field");
                }

                pos += BlockSize;

                // A metadata entry (PAX header or GNU long name) uses its own header size for its
                // content span. A file/directory entry may have that size overridden by a preceding
                // PAX "size" record (used when the real length does not fit the octal size field).
                bool isMetadata =
                    typeFlag == PaxExtendedHeaderTypeFlag ||
                    typeFlag == PaxGlobalHeaderTypeFlag ||
                    typeFlag == GnuLongNameTypeFlag ||
                    typeFlag == GnuLongLinkTypeFlag;

                long contentSize = isMetadata ? headerSize : (pendingSize ?? headerSize);
                if (contentSize < 0 || contentSize > int.MaxValue || pos + contentSize > tar.Length)
                {
                    return TarGzArchiveReadResult.Failure("a tar entry declares more content than the archive contains");
                }

                long contentByteSpan = ((contentSize + BlockSize - 1) / BlockSize) * BlockSize;

                if (typeFlag == PaxExtendedHeaderTypeFlag || typeFlag == PaxGlobalHeaderTypeFlag)
                {
                    ParsePaxRecords(tar, pos, (int)contentSize, ref pendingName, ref pendingSize);
                    pos += (int)contentByteSpan;
                    continue;
                }

                if (typeFlag == GnuLongNameTypeFlag)
                {
                    pendingName = ReadString(tar, pos, (int)contentSize);
                    pos += (int)contentByteSpan;
                    continue;
                }

                if (typeFlag == GnuLongLinkTypeFlag)
                {
                    // Long link targets are irrelevant to package files; consume and ignore.
                    pos += (int)contentByteSpan;
                    continue;
                }

                string effectiveName = pendingName ?? rawName;
                pendingName = null;
                pendingSize = null;

                byte[] content = new byte[contentSize];
                Array.Copy(tar, pos, content, 0, (int)contentSize);
                entries.Add(new TarArchiveEntry(effectiveName, typeFlag, content));

                pos += (int)contentByteSpan;
            }

            // A well-formed archive terminates with zero blocks. Reaching the end without a
            // terminator still yields the entries read so far; only header/size corruption is fatal.
            return TarGzArchiveReadResult.Ok(entries);
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
                value = (value * 10) + (b - (byte)'0');
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
    }
}
