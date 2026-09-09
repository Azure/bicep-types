// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Validation.Packaging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Packaging;

[TestClass]
public class PackageRelativePathTests
{
    [TestMethod]
    [DataRow("types.json", "types.json")]
    [DataRow("common/types.json", "common/types.json")]
    [DataRow("./types.json", "types.json")]
    [DataRow("common/./types.json", "common/types.json")]
    [DataRow("common/././types.json", "common/types.json")]
    [DataRow("common//types.json", "common/types.json")]
    [DataRow("common////types.json", "common/types.json")]
    [DataRow(@"common\types.json", "common/types.json")]
    [DataRow(@".\common//.\types.json", "common/types.json")]
    [DataRow(".metadata/types.json", ".metadata/types.json")]
    [DataRow("version.1/types.json", "version.1/types.json")]
    [DataRow("Types.json", "Types.json")]
    [DataRow("common/typ\u00e9s.json", "common/typ\u00e9s.json")]
    public void File_paths_are_canonicalized(string path, string expected)
    {
        var success = PackageRelativePath.TryCanonicalizeFile(path, out string canonical, out var error);

        success.Should().BeTrue();
        canonical.Should().Be(expected);
        error.Should().Be(PackageRelativePathError.None);
    }

    [TestMethod]
    public void Empty_reference_target_is_preserved_for_same_file_reference()
    {
        var success = PackageRelativePath.TryCanonicalizeReferenceTarget(
            string.Empty,
            out string canonical,
            out var error);

        success.Should().BeTrue();
        canonical.Should().BeEmpty();
        error.Should().Be(PackageRelativePathError.None);
    }

    [TestMethod]
    [DataRow("", "Empty")]
    [DataRow(".", "Empty")]
    [DataRow("././.", "Empty")]
    [DataRow("./", "TrailingSeparator")]
    [DataRow("types.json/", "TrailingSeparator")]
    [DataRow("/types.json", "Rooted")]
    [DataRow(@"\types.json", "Rooted")]
    [DataRow(@"\\server\share\types.json", "Rooted")]
    [DataRow(@"\\?\C:\types.json", "Rooted")]
    [DataRow("C:/types.json", "DriveQualified")]
    [DataRow("C:types.json", "DriveQualified")]
    [DataRow("../types.json", "ParentTraversal")]
    [DataRow("common/../types.json", "ParentTraversal")]
    [DataRow(@"common\..\types.json", "ParentTraversal")]
    [DataRow("a///../.././//../././types.json", "ParentTraversal")]
    public void Invalid_file_paths_are_rejected(string path, string expectedError)
    {
        var success = PackageRelativePath.TryCanonicalizeFile(path, out string canonical, out var error);

        success.Should().BeFalse();
        canonical.Should().BeEmpty();
        error.ToString().Should().Be(expectedError);
    }

    [TestMethod]
    [DataRow(".", "Empty")]
    [DataRow("./", "TrailingSeparator")]
    public void Nonempty_reference_target_cannot_collapse_to_same_file(
        string path,
        string expectedError)
    {
        var success = PackageRelativePath.TryCanonicalizeReferenceTarget(
            path,
            out string canonical,
            out var error);

        success.Should().BeFalse();
        canonical.Should().BeEmpty();
        error.ToString().Should().Be(expectedError);
    }

    [TestMethod]
    [DataRow("./C:/types.json")]
    [DataRow(".//C:types.json")]
    [DataRow("././1:types.json")]
    public void Drive_qualified_path_hidden_behind_safe_aliases_is_rejected(string path)
    {
        var success = PackageRelativePath.TryCanonicalizeFile(path, out string canonical, out var error);

        success.Should().BeFalse();
        canonical.Should().BeEmpty();
        error.Should().Be(PackageRelativePathError.DriveQualified);
    }

    [TestMethod]
    public void Canonicalization_is_idempotent_when_the_first_pass_succeeds()
    {
        var firstSuccess = PackageRelativePath.TryCanonicalizeFile(
            "./C:/types.json",
            out string firstCanonical,
            out _);

        if (!firstSuccess)
        {
            return;
        }

        PackageRelativePath.TryCanonicalizeFile(
            firstCanonical,
            out string secondCanonical,
            out _).Should().BeTrue();

        secondCanonical.Should().Be(firstCanonical);
    }
}