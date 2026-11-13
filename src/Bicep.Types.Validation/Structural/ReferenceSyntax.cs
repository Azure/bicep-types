// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Validates the syntax of reference objects (<c>{"$ref": "path#/index"}</c>) and
    /// produces structural diagnostics.  Uses <see cref="ReferencePath"/> for the actual
    /// string parsing so that <see cref="PackageReader"/> and structural validation share
    /// the same parser without coupling layers.
    /// </summary>
    internal static class ReferenceSyntax
    {
        /// <summary>
        /// Validates a JSON value node that is expected to be a reference object.
        /// Adds diagnostics to <paramref name="context"/> on failure and returns
        /// <see cref="ReferenceSyntaxResult.Invalid"/>.
        /// </summary>
        public static ReferenceSyntaxResult Validate(
            JsonValueNode node,
            string propertyName,
            string jsonPointer,
            StructuralValidationContext context)
        {
            if (node == null) { throw new ArgumentNullException(nameof(node)); }

            var doc = context.CurrentDocument;
            var sm = doc.SourceMap;
            var path = doc.PackageRelativePath;

            if (node.Kind != JsonValueKind.Object)
            {
                var loc = sm.GetLocation(node.ByteOffset);
                context.Add(TypeValidationDiagnosticBuilder.ReferenceObjectInvalid(
                    path, jsonPointer, propertyName,
                    $"expected an object with a '$ref' property, got {DescribeKind(node.Kind)}",
                    loc.Line, loc.Column));
                return ReferenceSyntaxResult.Invalid;
            }

            // Must have $ref
            if (!node.TryGetProperty("$ref", out var refNode))
            {
                var loc = sm.GetLocation(node.ByteOffset);
                context.Add(TypeValidationDiagnosticBuilder.ReferenceObjectInvalid(
                    path, jsonPointer, propertyName,
                    "the object is missing the required '$ref' property",
                    loc.Line, loc.Column));
                return ReferenceSyntaxResult.Invalid;
            }

            // $ref must be a string
            if (refNode.Kind != JsonValueKind.String)
            {
                var loc = sm.GetLocation(refNode.ByteOffset);
                context.Add(TypeValidationDiagnosticBuilder.ReferenceObjectInvalid(
                    path, jsonPointer, propertyName,
                    $"the '$ref' property must be a string, got {DescribeKind(refNode.Kind)}",
                    loc.Line, loc.Column));
                return ReferenceSyntaxResult.Invalid;
            }

            string refValue = refNode.StringValue ?? string.Empty;

            // Validate the $ref string syntax
            if (!ReferencePath.TryParse(refValue, out string packagePath, out int index))
            {
                var loc = sm.GetLocation(refNode.ByteOffset);
                string reason = DescribeRefSyntaxError(refValue);
                context.Add(TypeValidationDiagnosticBuilder.ReferenceSyntaxInvalid(
                    path, jsonPointer + "/$ref", refValue, reason, loc.Line, loc.Column));
                return ReferenceSyntaxResult.Invalid;
            }

            // Reject package paths that are not safe relative paths: rooted paths
            // ("/tmp/...", "C:/...") or paths containing ".." traversal (via '/' or '\').
            if (IsUnsafePackagePath(packagePath))
            {
                var loc = sm.GetLocation(refNode.ByteOffset);
                context.Add(TypeValidationDiagnosticBuilder.ReferenceSyntaxInvalid(
                    path, jsonPointer + "/$ref", refValue,
                    "the package path must be a relative path without '..' segments",
                    loc.Line, loc.Column));
                return ReferenceSyntaxResult.Invalid;
            }

            // Reject any property other than $ref on a reference object. A reference
            // object has no documented legacy fields, so this applies in both modes.
            foreach (var prop in node.Properties)
            {
                if (!string.Equals(prop.Name, "$ref", StringComparison.Ordinal))
                {
                    var loc = sm.GetLocation(prop.NameByteOffset);
                    context.Add(TypeValidationDiagnosticBuilder.UnknownProperty(
                        path, jsonPointer, prop.Name, loc.Line, loc.Column));
                }
            }

            return ReferenceSyntaxResult.Valid(packagePath, index);
        }

        private static bool IsUnsafePackagePath(string packagePath)
        {
            if (string.IsNullOrEmpty(packagePath)) { return false; }

            // Normalize Windows separators so both '/' and '\' traversal are checked.
            string normalized = packagePath.Replace('\\', '/');

            // Reject rooted paths: a leading '/' (POSIX / UNC) or a drive-qualified
            // path such as "C:/temp/...".
            if (normalized.StartsWith("/", StringComparison.Ordinal))
            {
                return true;
            }
            if (normalized.Length >= 2 && normalized[1] == ':')
            {
                return true;
            }

            // Reject any ".." traversal segment.
            foreach (string segment in normalized.Split('/'))
            {
                if (segment == "..") { return true; }
            }
            return false;
        }

        private static string DescribeKind(JsonValueKind kind) => JsonValueKindText.Describe(kind);

        private static string DescribeRefSyntaxError(string refValue)
        {
            if (string.IsNullOrEmpty(refValue))
            {
                return "the reference string is empty";
            }
            if (!refValue.Contains("#/"))
            {
                return "the reference string must contain a '#/' fragment separator";
            }
            int sep = refValue.IndexOf("#/", StringComparison.Ordinal);
            string indexText = refValue.Substring(sep + 2);
            if (string.IsNullOrEmpty(indexText))
            {
                return "the fragment index must be a non-negative integer";
            }
            if (int.TryParse(indexText, out int idx) && idx < 0)
            {
                return "the fragment index must be non-negative";
            }
            return "the reference string does not match the expected '<path>#/<index>' format";
        }
    }
}
