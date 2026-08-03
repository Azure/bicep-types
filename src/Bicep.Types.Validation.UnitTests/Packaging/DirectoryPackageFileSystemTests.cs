// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class DirectoryPackageFileSystemTests
{
    // ── Nonexistent root ────────────────────────────────────────────────────

    [TestMethod]
    public void Nonexistent_root_FileExists_returns_false()
    {
        var fs = new DirectoryPackageFileSystem("/path/does-not-exist-xyz");
        fs.FileExists("index.json").Should().BeFalse();
    }

    [TestMethod]
    public void Nonexistent_root_TryReadAllBytes_returns_false_with_error()
    {
        var fs = new DirectoryPackageFileSystem("/path/does-not-exist-xyz");
        var ok = fs.TryReadAllBytes("index.json", out _, out string error);
        ok.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    // ── Absolute package-relative path rejection ────────────────────────────

    [TestMethod]
    public void Absolute_package_relative_path_FileExists_returns_false()
    {
        using var dir = new TempDir();

        if (Path.DirectorySeparatorChar != '\\')
        {
            var misleadingWindowsPath = Path.Combine(dir.Path, "C:", "Windows", "System32");
            Directory.CreateDirectory(misleadingWindowsPath);
            File.WriteAllText(Path.Combine(misleadingWindowsPath, "cmd.exe"), string.Empty);
        }

        var fs = new DirectoryPackageFileSystem(dir.Path);

        // Package paths are portable, so both Unix and Windows absolute forms are always rejected.
        fs.FileExists("/etc/passwd").Should().BeFalse();
        fs.FileExists("C:\\Windows\\System32\\cmd.exe").Should().BeFalse();
    }

    [TestMethod]
    public void Absolute_package_relative_path_TryReadAllBytes_returns_false()
    {
        using var dir = new TempDir();
        var fs = new DirectoryPackageFileSystem(dir.Path);
        var ok = fs.TryReadAllBytes("/etc/passwd", out _, out _);
        ok.Should().BeFalse();
    }

    // ── Path traversal rejection ────────────────────────────────────────────

    [TestMethod]
    public void DotDot_traversal_FileExists_returns_false()
    {
        using var dir = new TempDir();
        var fs = new DirectoryPackageFileSystem(dir.Path);
        fs.FileExists("../outside-root.txt").Should().BeFalse();
    }

    [TestMethod]
    public void DotDot_traversal_TryReadAllBytes_returns_false()
    {
        using var dir = new TempDir();
        var fs = new DirectoryPackageFileSystem(dir.Path);
        var ok = fs.TryReadAllBytes("../../secret.txt", out _, out _);
        ok.Should().BeFalse();
    }

    [TestMethod]
    public void Case_variant_sibling_does_not_pass_root_containment_on_case_sensitive_file_systems()
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            return;
        }

        using var parent = new TempDir();
        var root = Path.Combine(parent.Path, "package");
        var sibling = Path.Combine(parent.Path, "PACKAGE");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(sibling);
        File.WriteAllText(Path.Combine(sibling, "secret.txt"), string.Empty);

        var fs = new DirectoryPackageFileSystem(root);

        fs.FileExists("../PACKAGE/secret.txt").Should().BeFalse();
    }

    // ── Separator normalization ─────────────────────────────────────────────

    [TestMethod]
    public void Backslash_and_slash_separators_normalize_for_package_identity()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(Path.Combine(dir.Path, "sub"));
        File.WriteAllText(Path.Combine(dir.Path, "sub", "types.json"), "[]");

        var fs = new DirectoryPackageFileSystem(dir.Path);

        // Both slash variants should resolve to the same file
        fs.FileExists("sub/types.json").Should().BeTrue();
        fs.FileExists(@"sub\types.json").Should().BeTrue();
    }

    // ── Normal reads ────────────────────────────────────────────────────────

    [TestMethod]
    public void Existing_file_is_read_with_deterministic_utf8_bytes()
    {
        using var dir = new TempDir();
        const string content = "{\"hello\":\"wörld\"}";
        byte[] expected = System.Text.Encoding.UTF8.GetBytes(content);
        File.WriteAllBytes(Path.Combine(dir.Path, "data.json"), expected);

        var fs = new DirectoryPackageFileSystem(dir.Path);
        var ok = fs.TryReadAllBytes("data.json", out byte[] actual, out _);

        ok.Should().BeTrue();
        actual.Should().Equal(expected);
    }

    // ── Trailing separator on root ──────────────────────────────────────────

    [TestMethod]
    public void Root_with_trailing_separator_still_resolves_child_files()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "index.json"), "{}");

        // Supply the root with a trailing directory separator.
        var rootWithSlash = dir.Path + Path.DirectorySeparatorChar;
        var fs = new DirectoryPackageFileSystem(rootWithSlash);

        fs.FileExists("index.json").Should().BeTrue();
        fs.TryReadAllBytes("index.json", out _, out _).Should().BeTrue();
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bcpvt-fstest-" + System.IO.Path.GetRandomFileName());

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best-effort */ }
        }
    }
}
