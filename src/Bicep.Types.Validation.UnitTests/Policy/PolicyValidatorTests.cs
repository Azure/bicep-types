// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.UnitTests.Graph;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Bicep.Types.Validation.UnitTests.Policy;

[TestClass]
public class PolicyValidatorTests
{
    private static readonly TypePackageValidator Validator = new();

    [TestMethod]
    public void Policy_scans_unreferenced_nodes_in_reached_type_files()
    {
        // The index reaches /0; /1 is a BuiltInType that no reference points to. Policy inspects
        // every structurally usable element of a reached file, so /1 is still classified.
        const string types =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/2\"},\"readableScopes\":8,\"writableScopes\":8}," +
            "{\"$type\":\"BuiltInType\",\"kind\":5}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}]";
        var fs = new InMemoryPackageFileSystem().AddText("types.json", types);

        PolicyTestHelpers.RunPolicy(PolicyTestHelpers.ResourceIndex("types.json#/0"), fs,
            TypePackageValidationMode.CanonicalWriter)
            .Should().ContainSingle(d => d.Code == TypeValidationDiagnosticCodes.CanonicalFormViolation)
            .Which.JsonPointer.Should().Be("/1/kind");
    }

    [TestMethod]
    public void Unreached_type_files_are_not_scanned_by_policy()
    {
        // types.json is reached and clean; other.json contains a legacy field but is never
        // referenced, so policy must not scan it.
        const string types =
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"readableScopes\":8,\"writableScopes\":8}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}]";
        const string other =
            "[{\"$type\":\"ResourceType\",\"name\":\"Other/y@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"scopeType\":0}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}]";
        var fs = new InMemoryPackageFileSystem()
            .AddText("types.json", types)
            .AddText("other.json", other);

        PolicyTestHelpers.RunPolicy(PolicyTestHelpers.ResourceIndex("types.json#/0"), fs,
            TypePackageValidationMode.CanonicalWriter)
            .Should().BeEmpty();
    }

    // ── Result-shaping lock-in tests (through the full validator) ─────────────

    [TestMethod]
    public void Compatibility_legacy_field_produces_single_warning_and_valid_result()
    {
        using var pkg = LegacyScopePackage();
        var options = new TypePackageValidationOptions { Mode = TypePackageValidationMode.CompatibleReader };

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path), options);

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(TypeValidationDiagnosticCodes.CompatibilityFormUsed);
        result.Summary.WarningCount.Should().Be(1);
        result.Summary.ErrorCount.Should().Be(0);
    }

    [TestMethod]
    public void Compatibility_warning_excluded_when_include_warnings_false_but_result_stays_valid()
    {
        using var pkg = LegacyScopePackage();
        var options = new TypePackageValidationOptions
        {
            Mode = TypePackageValidationMode.CompatibleReader,
            IncludeWarnings = false,
        };

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path), options);

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
        // Summary still counts the suppressed warning.
        result.Summary.WarningCount.Should().Be(1);
    }

    [TestMethod]
    public void Canonical_legacy_field_produces_policy_error()
    {
        using var pkg = LegacyScopePackage();
        var options = new TypePackageValidationOptions { Mode = TypePackageValidationMode.CanonicalWriter };

        var result = Validator.Validate(TypePackageValidationInput.ForDirectory(pkg.Path), options);

        result.IsValid.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Code == TypeValidationDiagnosticCodes.CanonicalFormViolation);
    }

    /// <summary>
    /// A package whose single resource type uses only the legacy <c>scopeType</c> scope field.
    /// In CompatibleReader the modern pair is not required, yielding a clean single-warning result.
    /// </summary>
    private static TempDir LegacyScopePackage()
    {
        var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "index.json"),
            "{\"resources\":{\"My.Rp/x@2026-01-01\":{\"$ref\":\"types.json#/0\"}}," +
            "\"resourceFunctions\":{},\"namespaceFunctions\":[]}");
        File.WriteAllText(Path.Combine(dir.Path, "types.json"),
            "[{\"$type\":\"ResourceType\",\"name\":\"My.Rp/x@2026-01-01\"," +
            "\"body\":{\"$ref\":\"#/1\"},\"scopeType\":0}," +
            "{\"$type\":\"ObjectType\",\"name\":\"o\",\"properties\":{}}]");
        return dir;
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bcpvt-policy-test-" + System.IO.Path.GetRandomFileName());

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best-effort */ }
        }
    }
}
