// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.Policy;

namespace Azure.Bicep.Types.Validation.Semantic
{
    /// <summary>
    /// Scalar-semantic layer.  Runs after semantic-graph validation and before mode policy, over
    /// the type files graph traversal reached.  It validates value-domain constraints that the
    /// structural layer intentionally does not check: numeric range ordering, non-negative length
    /// bounds, and enum/flags value domains.  Like the policy layer it reads the raw JSON node
    /// model (never the deserialized type model) so it can report precise source locations.
    /// </summary>
    /// <remarks>
    /// Range and length rules (BCPVT026/BCPVT027) are unconditional errors.  Enum and flags domain
    /// rules (BCPVT028/BCPVT029) are mode-aware: an <c>Error</c> in <c>CanonicalWriter</c> and a
    /// <c>Warning</c> in <c>CompatibleReader</c>, because new enum values and flag bits are
    /// backward-compatible model evolution that a compatible reader must tolerate.  Direct-root
    /// legacy <c>ResourceType</c> scope fields (<c>scopeType</c>/<c>readOnlyScopes</c>/<c>flags</c>)
    /// are owned by the mode-policy layer and are never read here.  Wrong-shape (non-integer)
    /// fields are owned by the structural layer and are skipped.
    /// </remarks>
    internal static class ScalarSemanticValidator
    {
        // Known masks and value sets from the frozen Bicep.Types model.
        private const long ScopeTypeMask = 31;                        // ScopeType.All
        private const long ObjectTypePropertyFlagsMask = 31;          // Required|ReadOnly|WriteOnly|DeployTimeConstant|Identifier
        private const long NamespaceFunctionParameterFlagsMask = 7;   // Required|CompileTimeConstant|DeployTimeConstant
        private static readonly long[] BicepSourceFileKindValues = { 1, 2 }; // BicepFile, ParamsFile

        public static IReadOnlyList<TypeValidationDiagnostic> Validate(
            IEnumerable<PackageDocumentProviderResult> reachedTypeFiles,
            TypePackageValidationOptions options)
        {
            if (reachedTypeFiles == null) { throw new ArgumentNullException(nameof(reachedTypeFiles)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }

            var diagnostics = new List<TypeValidationDiagnostic>();

            // Enum/flags domain violations are backward-compatible evolution: errors for a
            // canonical writer, warnings for a compatible reader.
            var domainSeverity = options.Mode == TypePackageValidationMode.CanonicalWriter
                ? TypeValidationDiagnosticSeverity.Error
                : TypeValidationDiagnosticSeverity.Warning;

            foreach (var file in reachedTypeFiles)
            {
                foreach (var node in file.NodesByIndex)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    switch (node.Discriminator)
                    {
                        case "IntegerType":
                            ValidateNumericRange(node, "IntegerType", "minValue", "maxValue", diagnostics);
                            break;

                        case "StringType":
                            ValidateNumericRange(node, "StringType", "minLength", "maxLength", diagnostics);
                            ValidateNonNegativeLength(node, "StringType", "minLength", diagnostics);
                            ValidateNonNegativeLength(node, "StringType", "maxLength", diagnostics);
                            break;

                        case "ArrayType":
                            ValidateNumericRange(node, "ArrayType", "minLength", "maxLength", diagnostics);
                            ValidateNonNegativeLength(node, "ArrayType", "minLength", diagnostics);
                            ValidateNonNegativeLength(node, "ArrayType", "maxLength", diagnostics);
                            break;

                        case "ResourceType":
                            // Only the modern scope pair participates here; the legacy direct-root
                            // scope fields are owned by the mode-policy layer.
                            ValidateFlagsField(node, "readableScopes", ScopeTypeMask, "ResourceType readableScopes", domainSeverity, diagnostics);
                            ValidateFlagsField(node, "writableScopes", ScopeTypeMask, "ResourceType writableScopes", domainSeverity, diagnostics);
                            break;

                        case "ObjectType":
                            ValidatePropertyMapFlags(node, "properties", ObjectTypePropertyFlagsMask, "ObjectType property flags", domainSeverity, diagnostics);
                            break;

                        case "DiscriminatedObjectType":
                            ValidatePropertyMapFlags(node, "baseProperties", ObjectTypePropertyFlagsMask, "DiscriminatedObjectType base property flags", domainSeverity, diagnostics);
                            break;

                        case "NamespaceFunctionType":
                            ValidateParameterArrayFlags(node, "parameters", NamespaceFunctionParameterFlagsMask, "NamespaceFunctionType parameter flags", domainSeverity, diagnostics);
                            ValidateEnumField(node, "visibleInFileKind", BicepSourceFileKindValues, "NamespaceFunctionType.visibleInFileKind", domainSeverity, diagnostics);
                            break;
                    }
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Reports BCPVT026 when both range bounds are present, shape-valid integers, and the
        /// minimum exceeds the maximum.  The diagnostic points at the maximum field so baselines
        /// do not depend on source property order.
        /// </summary>
        private static void ValidateNumericRange(
            TypeGraphNode node, string typeName, string minFieldName, string maxFieldName,
            List<TypeValidationDiagnostic> diagnostics)
        {
            var obj = node.ObjectNode;
            if (!PolicyNodeReader.TryGetIntegerProperty(obj, minFieldName, out _, out long min)) { return; }
            if (!PolicyNodeReader.TryGetIntegerProperty(obj, maxFieldName, out var maxProperty, out long max)) { return; }
            if (min <= max) { return; }

            var location = node.Document.SourceMap.GetLocation(maxProperty.NameByteOffset);
            diagnostics.Add(TypeValidationDiagnosticBuilder.NumericRangeInvalid(
                node.Document.PackageRelativePath, node.JsonPointer, typeName,
                minFieldName, min, maxFieldName, max, location.Line, location.Column));
        }

        /// <summary>Reports BCPVT027 when a present, shape-valid length field is negative.</summary>
        private static void ValidateNonNegativeLength(
            TypeGraphNode node, string typeName, string fieldName, List<TypeValidationDiagnostic> diagnostics)
        {
            var obj = node.ObjectNode;
            if (!PolicyNodeReader.TryGetIntegerProperty(obj, fieldName, out var property, out long value)) { return; }
            if (value >= 0) { return; }

            var location = node.Document.SourceMap.GetLocation(property.NameByteOffset);
            string pointer = node.JsonPointer + "/" + fieldName;
            diagnostics.Add(TypeValidationDiagnosticBuilder.LengthConstraintNegative(
                node.Document.PackageRelativePath, pointer, typeName, fieldName, value, location.Line, location.Column));
        }

        /// <summary>Reports BCPVT029 when a present, shape-valid direct-root flags field carries bits outside <paramref name="mask"/>.</summary>
        private static void ValidateFlagsField(
            TypeGraphNode node, string fieldName, long mask, string description,
            TypeValidationDiagnosticSeverity severity, List<TypeValidationDiagnostic> diagnostics)
        {
            var obj = node.ObjectNode;
            if (!PolicyNodeReader.TryGetIntegerProperty(obj, fieldName, out var property, out long value)) { return; }
            long unknownBits = value & ~mask;
            if (unknownBits == 0) { return; }

            var location = node.Document.SourceMap.GetLocation(property.NameByteOffset);
            string pointer = node.JsonPointer + "/" + fieldName;
            diagnostics.Add(TypeValidationDiagnosticBuilder.FlagsValueInvalid(
                node.Document.PackageRelativePath, pointer, description, unknownBits, mask, severity, location.Line, location.Column));
        }

        /// <summary>Reports BCPVT029 for each object-map member whose nested <c>flags</c> field carries bits outside <paramref name="mask"/>.</summary>
        private static void ValidatePropertyMapFlags(
            TypeGraphNode node, string mapFieldName, long mask, string description,
            TypeValidationDiagnosticSeverity severity, List<TypeValidationDiagnostic> diagnostics)
        {
            var obj = node.ObjectNode;
            if (!obj.TryGetProperty(mapFieldName, out var map) || map.Kind != JsonValueKind.Object) { return; }

            string mapPointer = node.JsonPointer + "/" + mapFieldName;
            var sourceMap = node.Document.SourceMap;
            string path = node.Document.PackageRelativePath;

            foreach (var member in map.Properties)
            {
                if (member.Value.Kind != JsonValueKind.Object) { continue; }
                if (!PolicyNodeReader.TryGetIntegerProperty(member.Value, "flags", out var flagsProperty, out long value)) { continue; }
                long unknownBits = value & ~mask;
                if (unknownBits == 0) { continue; }

                var location = sourceMap.GetLocation(flagsProperty.NameByteOffset);
                string pointer = mapPointer + "/" + JsonPointerEscape(member.Name) + "/flags";
                diagnostics.Add(TypeValidationDiagnosticBuilder.FlagsValueInvalid(
                    path, pointer, description, unknownBits, mask, severity, location.Line, location.Column));
            }
        }

        /// <summary>Reports BCPVT029 for each parameter element whose <c>flags</c> field carries bits outside <paramref name="mask"/>.</summary>
        private static void ValidateParameterArrayFlags(
            TypeGraphNode node, string arrayFieldName, long mask, string description,
            TypeValidationDiagnosticSeverity severity, List<TypeValidationDiagnostic> diagnostics)
        {
            var obj = node.ObjectNode;
            if (!obj.TryGetProperty(arrayFieldName, out var array) || array.Kind != JsonValueKind.Array) { return; }

            string arrayPointer = node.JsonPointer + "/" + arrayFieldName;
            var sourceMap = node.Document.SourceMap;
            string path = node.Document.PackageRelativePath;

            for (int i = 0; i < array.Elements.Count; i++)
            {
                var element = array.Elements[i];
                if (element.Kind != JsonValueKind.Object) { continue; }
                if (!PolicyNodeReader.TryGetIntegerProperty(element, "flags", out var flagsProperty, out long value)) { continue; }
                long unknownBits = value & ~mask;
                if (unknownBits == 0) { continue; }

                var location = sourceMap.GetLocation(flagsProperty.NameByteOffset);
                string pointer = arrayPointer + "/" + i + "/flags";
                diagnostics.Add(TypeValidationDiagnosticBuilder.FlagsValueInvalid(
                    path, pointer, description, unknownBits, mask, severity, location.Line, location.Column));
            }
        }

        /// <summary>Reports BCPVT028 when a present, shape-valid enum field is outside its documented value set.</summary>
        private static void ValidateEnumField(
            TypeGraphNode node, string fieldName, long[] allowedValues, string qualifiedFieldName,
            TypeValidationDiagnosticSeverity severity, List<TypeValidationDiagnostic> diagnostics)
        {
            var obj = node.ObjectNode;
            if (!PolicyNodeReader.TryGetIntegerProperty(obj, fieldName, out var property, out long value)) { return; }

            foreach (long allowed in allowedValues)
            {
                if (value == allowed) { return; }
            }

            var location = node.Document.SourceMap.GetLocation(property.NameByteOffset);
            string pointer = node.JsonPointer + "/" + fieldName;
            diagnostics.Add(TypeValidationDiagnosticBuilder.EnumValueInvalid(
                node.Document.PackageRelativePath, pointer, qualifiedFieldName, FormatAllowed(allowedValues), value, severity, location.Line, location.Column));
        }

        private static string FormatAllowed(long[] values)
        {
            string result = string.Empty;
            for (int i = 0; i < values.Length; i++)
            {
                string text = values[i].ToString(CultureInfo.InvariantCulture);
                if (i == 0)
                {
                    result = text;
                }
                else if (i == values.Length - 1)
                {
                    result = result + " or " + text;
                }
                else
                {
                    result = result + ", " + text;
                }
            }
            return result;
        }

        private static string JsonPointerEscape(string token)
        {
            return token.Replace("~", "~0").Replace("/", "~1");
        }
    }
}
