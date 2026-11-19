// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

/// <summary>
/// A single entry to place into a test tar archive.
/// </summary>
internal readonly struct TarGzTestEntry
{
    public TarGzTestEntry(string name, byte[] content, byte typeFlag = (byte)'0')
    {
        Name = name;
        Content = content;
        TypeFlag = typeFlag;
    }

    public string Name { get; }

    public byte[] Content { get; }

    public byte TypeFlag { get; }

    public static TarGzTestEntry File(string name, string content) =>
        new TarGzTestEntry(name, Encoding.UTF8.GetBytes(content));

    public static TarGzTestEntry Directory(string name) =>
        new TarGzTestEntry(name, Array.Empty<byte>(), (byte)'5');

    public static TarGzTestEntry Symlink(string name) =>
        new TarGzTestEntry(name, Array.Empty<byte>(), (byte)'2');
}

/// <summary>
/// Builds tiny gzip-compressed ustar archives for archive-input tests, producing correct headers,
/// octal size fields, and checksums so the product reader accepts valid archives.
/// </summary>
internal static class TarGzTestArchive
{
    private const int BlockSize = 512;

    /// <summary>Builds a gzip-compressed tar from text file entries.</summary>
    public static byte[] FromTextFiles(params (string name, string content)[] files)
    {
        var entries = new List<TarGzTestEntry>();
        foreach (var (name, content) in files)
        {
            entries.Add(TarGzTestEntry.File(name, content));
        }
        return Build(entries);
    }

    /// <summary>Builds a gzip-compressed tar from arbitrary entries.</summary>
    public static byte[] Build(IEnumerable<TarGzTestEntry> entries)
    {
        byte[] tar = BuildTar(entries);
        return GzipCompress(tar);
    }

    /// <summary>Builds an uncompressed tar (for producing malformed-container fixtures).</summary>
    public static byte[] BuildTar(IEnumerable<TarGzTestEntry> entries)
    {
        using var stream = new MemoryStream();
        foreach (var entry in entries)
        {
            WriteEntry(stream, entry);
        }

        // Two zero blocks terminate the archive.
        stream.Write(new byte[BlockSize * 2], 0, BlockSize * 2);
        return stream.ToArray();
    }

    /// <summary>Gzip-compresses raw bytes.</summary>
    public static byte[] GzipCompress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    private static void WriteEntry(Stream stream, TarGzTestEntry entry)
    {
        var header = new byte[BlockSize];
        byte[] nameBytes = Encoding.ASCII.GetBytes(entry.Name);
        Array.Copy(nameBytes, 0, header, 0, Math.Min(nameBytes.Length, 100));

        WriteOctal(header, 100, 8, 0b_110_100_100); // mode 0644
        WriteOctal(header, 108, 8, 0); // uid
        WriteOctal(header, 116, 8, 0); // gid
        WriteOctal(header, 124, 12, entry.Content.Length); // size
        WriteOctal(header, 136, 12, 0); // mtime

        header[156] = entry.TypeFlag;

        // ustar magic + version.
        byte[] magic = Encoding.ASCII.GetBytes("ustar");
        Array.Copy(magic, 0, header, 257, magic.Length);
        header[263] = (byte)'0';
        header[264] = (byte)'0';

        WriteChecksum(header);

        stream.Write(header, 0, BlockSize);

        if (entry.Content.Length > 0)
        {
            stream.Write(entry.Content, 0, entry.Content.Length);
            int remainder = entry.Content.Length % BlockSize;
            if (remainder != 0)
            {
                stream.Write(new byte[BlockSize - remainder], 0, BlockSize - remainder);
            }
        }
    }

    private static void WriteOctal(byte[] header, int offset, int length, long value)
    {
        // length-1 octal digits, zero-padded, followed by a NUL terminator.
        string text = Convert.ToString(value, 8).PadLeft(length - 1, '0');
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        Array.Copy(bytes, 0, header, offset, length - 1);
        header[offset + length - 1] = 0;
    }

    private static void WriteChecksum(byte[] header)
    {
        // Checksum is computed with the checksum field treated as 8 spaces.
        for (int i = 148; i < 156; i++)
        {
            header[i] = (byte)' ';
        }

        long sum = 0;
        for (int i = 0; i < BlockSize; i++)
        {
            sum += header[i];
        }

        // Six octal digits, NUL, then a space.
        string text = Convert.ToString(sum, 8).PadLeft(6, '0');
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        Array.Copy(bytes, 0, header, 148, 6);
        header[154] = 0;
        header[155] = (byte)' ';
    }
}
