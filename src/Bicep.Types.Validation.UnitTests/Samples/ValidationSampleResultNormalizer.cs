// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Bicep.Types.Validation.Diagnostics;

namespace Azure.Bicep.Types.Validation.UnitTests.Samples;

/// <summary>
/// Converts a <see cref="TypePackageValidationResult"/> into the stable JSON baseline shape,
/// and canonicalizes expected baseline text through the same serializer for comparison.
/// </summary>
public static class ValidationSampleResultNormalizer
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    /// <summary>Serializes a result into the deterministic baseline JSON shape.</summary>
    /// <param name="result">The validation result to normalize.</param>
    /// <param name="temporaryRoot">
    /// When provided, occurrences of this path (or its forward-slash equivalent) in diagnostic
    /// messages are replaced with the stable placeholder <c>&lt;sample-root&gt;</c> so that
    /// baselines are reproducible across machines and temp-directory locations.
    /// </param>
    public static string Normalize(TypePackageValidationResult result, string? temporaryRoot = null)
    {
        var root = new JsonObject
        {
            ["isValid"] = result.IsValid,
            ["mode"] = ModeToString(result.Mode),
            ["diagnostics"] = BuildDiagnostics(result.Diagnostics, temporaryRoot),
            ["diagnosticsTruncated"] = result.DiagnosticsTruncated,
            ["summary"] = new JsonObject
            {
                ["errorCount"] = result.Summary.ErrorCount,
                ["warningCount"] = result.Summary.WarningCount,
                ["infoCount"] = result.Summary.InfoCount,
            },
        };

        return root.ToJsonString(SerializerOptions);
    }

    /// <summary>Re-serializes expected baseline text through the same serializer to normalize formatting.</summary>
    public static string Canonicalize(string json)
    {
        var node = JsonNode.Parse(json)
            ?? throw new InvalidOperationException("Expected baseline JSON parsed to null.");
        return node.ToJsonString(SerializerOptions);
    }

    private static JsonArray BuildDiagnostics(IReadOnlyList<TypeValidationDiagnostic> diagnostics, string? temporaryRoot)
    {
        var array = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            var obj = new JsonObject
            {
                ["code"] = diagnostic.Code,
                ["severity"] = SeverityToString(diagnostic.Severity),
                ["message"] = RedactPath(diagnostic.Message, temporaryRoot),
            };

            if (!string.IsNullOrEmpty(diagnostic.Path))
            {
                obj["path"] = diagnostic.Path;
            }

            if (!string.IsNullOrEmpty(diagnostic.JsonPointer))
            {
                obj["jsonPointer"] = diagnostic.JsonPointer;
            }

            if (diagnostic.Line.HasValue)
            {
                obj["line"] = diagnostic.Line.Value;
            }

            if (diagnostic.Column.HasValue)
            {
                obj["column"] = diagnostic.Column.Value;
            }

            if (diagnostic.RelatedLocations.Count > 0)
            {
                obj["relatedLocations"] = BuildRelatedLocations(diagnostic.RelatedLocations);
            }

            array.Add(obj);
        }

        return array;
    }

    private static JsonArray BuildRelatedLocations(IReadOnlyList<TypeValidationDiagnosticRelatedLocation> related)
    {
        var array = new JsonArray();
        foreach (var location in related)
        {
            var obj = new JsonObject
            {
                ["message"] = location.Message,
            };

            if (!string.IsNullOrEmpty(location.Path))
            {
                obj["path"] = location.Path;
            }

            if (!string.IsNullOrEmpty(location.JsonPointer))
            {
                obj["jsonPointer"] = location.JsonPointer;
            }

            if (location.Line.HasValue)
            {
                obj["line"] = location.Line.Value;
            }

            if (location.Column.HasValue)
            {
                obj["column"] = location.Column.Value;
            }

            array.Add(obj);
        }

        return array;
    }

    private static string ModeToString(TypePackageValidationMode mode) => mode switch
    {
        TypePackageValidationMode.CanonicalWriter => "canonicalWriter",
        TypePackageValidationMode.CompatibleReader => "compatibleReader",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown validation mode."),
    };

    private static string SeverityToString(TypeValidationDiagnosticSeverity severity) => severity switch
    {
        TypeValidationDiagnosticSeverity.Error => "error",
        TypeValidationDiagnosticSeverity.Warning => "warning",
        TypeValidationDiagnosticSeverity.Info => "info",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity."),
    };

    private static string RedactPath(string message, string? temporaryRoot)
    {
        if (string.IsNullOrEmpty(temporaryRoot) || string.IsNullOrEmpty(message))
        {
            return message;
        }

        // Replace both backslash and forward-slash variants of the temp root
        var withForwardSlash = temporaryRoot.Replace('\\', '/');
        var result = message
            .Replace(temporaryRoot, "<sample-root>", StringComparison.Ordinal)
            .Replace(withForwardSlash, "<sample-root>", StringComparison.Ordinal);

        // Normalize any remaining backslash path separators that follow the placeholder
        // so baselines are cross-platform stable.
        return result.Replace("<sample-root>\\", "<sample-root>/", StringComparison.Ordinal);
    }
}
