// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Bicep.Types.Validation.Diagnostics;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Archive-backed implementation of <see cref="IPackageFileSystem"/>.  Regular-file entries from a
    /// gzip-compressed tar (<c>types.tgz</c>) are read into memory and keyed by their canonical
    /// package-relative path so downstream validators cannot tell whether files came from a directory
    /// or an archive.  Container defects surface as diagnostics rather than exceptions.
    /// </summary>
    internal sealed class ArchivePackageFileSystem : IPackageFileSystem
    {
        // Regular-file tar typeflags: '0' for POSIX regular files and NUL for the historical form.
        private const byte RegularFileTypeFlag = (byte)'0';
        private const byte AlternateRegularFileTypeFlag = 0;
        private const byte DirectoryTypeFlag = (byte)'5';

        private readonly Dictionary<string, ArchiveMember> membersByKey;

        private ArchivePackageFileSystem(
            Dictionary<string, ArchiveMember> membersByKey,
            IReadOnlyList<TypeValidationDiagnostic> diagnostics)
        {
            this.membersByKey = membersByKey;
            Diagnostics = diagnostics;
        }

        /// <summary>Container diagnostics accumulated while reading the archive.</summary>
        public IReadOnlyList<TypeValidationDiagnostic> Diagnostics { get; }

        /// <summary>
        /// <c>true</c> when the archive could not be read as a self-contained package, either because the
        /// gzip/tar container is malformed or because a member is unsafe, duplicated, or collides.
        /// </summary>
        public bool HasFatalContainerFailure => Diagnostics.Count > 0;

        /// <summary>Reads an archive from its raw bytes, classifying entries and validating member paths.</summary>
        public static ArchivePackageFileSystem Create(byte[] archiveBytes, string displayPath)
        {
            if (archiveBytes == null) { throw new ArgumentNullException(nameof(archiveBytes)); }
            if (displayPath == null) { throw new ArgumentNullException(nameof(displayPath)); }

            var readResult = TarGzArchiveReader.Read(archiveBytes);
            if (!readResult.Success)
            {
                // A gzip/tar structural failure is a single fatal diagnostic with no member-level detail.
                var fatal = new[] { TypeValidationDiagnosticBuilder.ArchivePackageInvalid(displayPath, readResult.ErrorMessage!) };
                return new ArchivePackageFileSystem(new Dictionary<string, ArchiveMember>(StringComparer.OrdinalIgnoreCase), fatal);
            }

            var members = new Dictionary<string, ArchiveMember>(StringComparer.OrdinalIgnoreCase);
            var diagnostics = new List<TypeValidationDiagnostic>();

            foreach (var entry in readResult.Entries)
            {
                if (entry.TypeFlag == DirectoryTypeFlag)
                {
                    // Directory entries carry no file content and never enter the package file map.
                    continue;
                }

                if (entry.TypeFlag != RegularFileTypeFlag && entry.TypeFlag != AlternateRegularFileTypeFlag)
                {
                    diagnostics.Add(TypeValidationDiagnosticBuilder.ArchiveMemberEntryTypeUnsupported(
                        entry.RawName, DescribeEntryType(entry.TypeFlag)));
                    continue;
                }

                if (!TryCanonicalize(entry.RawName, out string canonical))
                {
                    diagnostics.Add(TypeValidationDiagnosticBuilder.ArchiveMemberPathInvalid(entry.RawName));
                    continue;
                }

                if (members.TryGetValue(canonical, out var existing))
                {
                    if (string.Equals(existing.RawName, entry.RawName, StringComparison.Ordinal))
                    {
                        // The identical raw member name appears more than once.
                        diagnostics.Add(TypeValidationDiagnosticBuilder.ArchiveMemberDuplicate(displayPath, entry.RawName));
                    }
                    else
                    {
                        // Distinct raw names that normalize or compare equal (leading "./" alias or
                        // case-only difference) collide under the package-path comparer.
                        diagnostics.Add(TypeValidationDiagnosticBuilder.ArchiveMemberPathCollision(existing.RawName, entry.RawName));
                    }
                    continue;
                }

                members[canonical] = new ArchiveMember(canonical, entry.RawName, entry.Content);
            }

            return new ArchivePackageFileSystem(members, diagnostics);
        }

        /// <inheritdoc/>
        public bool FileExists(string packageRelativePath)
        {
            return TryNormalizeLookup(packageRelativePath, out string key) && membersByKey.ContainsKey(key);
        }

        /// <inheritdoc/>
        public bool TryReadAllBytes(string packageRelativePath, out byte[] bytes, out string error)
        {
            if (TryNormalizeLookup(packageRelativePath, out string key) && membersByKey.TryGetValue(key, out var member))
            {
                bytes = member.Content;
                error = string.Empty;
                return true;
            }

            bytes = Array.Empty<byte>();
            error = $"Archive member '{packageRelativePath}' was not found in the package.";
            return false;
        }

        /// <inheritdoc/>
        public IEnumerable<string> EnumerateFiles()
        {
            return membersByKey.Values
                .Select(m => m.Canonical)
                .OrderBy(p => p, StringComparer.Ordinal);
        }

        /// <summary>
        /// Validates a raw archive member name as a package-relative path and produces its canonical form.
        /// </summary>
        private static bool TryCanonicalize(string rawName, out string canonical)
        {
            canonical = string.Empty;
            if (string.IsNullOrEmpty(rawName))
            {
                return false;
            }

            // Backslashes are never valid canonical archive member characters.
            if (rawName.IndexOf('\\') >= 0)
            {
                return false;
            }

            string name = rawName;

            // A single leading "./" prefix is accepted for tar-writer interoperability and stripped.
            if (name.StartsWith("./", StringComparison.Ordinal))
            {
                name = name.Substring(2);
            }

            if (name.Length == 0)
            {
                return false;
            }

            // Reject Unix-rooted paths.
            if (name[0] == '/')
            {
                return false;
            }

            // Reject Windows drive-rooted paths such as "C:/types.json".
            if (name.Length >= 2 && name[1] == ':')
            {
                return false;
            }

            var segments = name.Split('/');
            foreach (var segment in segments)
            {
                if (segment.Length == 0)
                {
                    // Empty segment: leading, trailing, or doubled slash.
                    return false;
                }
                if (string.Equals(segment, ".", StringComparison.Ordinal) ||
                    string.Equals(segment, "..", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            canonical = name;
            return true;
        }

        private static bool TryNormalizeLookup(string packageRelativePath, out string key)
        {
            key = string.Empty;
            if (string.IsNullOrEmpty(packageRelativePath))
            {
                return false;
            }

            string name = packageRelativePath.Replace('\\', '/');
            if (name.StartsWith("./", StringComparison.Ordinal))
            {
                name = name.Substring(2);
            }

            if (name.Length == 0)
            {
                return false;
            }

            key = name;
            return true;
        }

        private static string DescribeEntryType(byte typeFlag)
        {
            switch (typeFlag)
            {
                case (byte)'1': return "HardLink";
                case (byte)'2': return "SymbolicLink";
                case (byte)'3': return "CharacterDevice";
                case (byte)'4': return "BlockDevice";
                case (byte)'6': return "Fifo";
                case (byte)'7': return "ContiguousFile";
                case (byte)'g': return "GlobalExtendedHeader";
                case (byte)'x': return "ExtendedHeader";
                case (byte)'L': return "GnuLongName";
                case (byte)'K': return "GnuLongLink";
                default: return "Unsupported";
            }
        }

        private readonly struct ArchiveMember
        {
            public ArchiveMember(string canonical, string rawName, byte[] content)
            {
                Canonical = canonical;
                RawName = rawName;
                Content = content;
            }

            public string Canonical { get; }

            public string RawName { get; }

            public byte[] Content { get; }
        }
    }
}
