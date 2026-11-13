// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Policy
{
    /// <summary>
    /// Policy for <c>ResourceType</c> scope fields.  The legacy scope fields <c>scopeType</c>,
    /// <c>readOnlyScopes</c> and <c>flags</c> are rejected in <c>CanonicalWriter</c> (BCPVT022)
    /// and accepted with a warning in <c>CompatibleReader</c> (BCPVT023).  When a package mixes
    /// the modern scope pair with an <em>effective</em> legacy scope field, a single BCPVT024 is
    /// emitted for the object and the per-field classification is suppressed.
    /// </summary>
    /// <remarks>
    /// Only the direct root fields of the <c>ResourceType</c> object are considered; property-level
    /// or parameter-level <c>flags</c> elsewhere in the document are never read here.
    /// </remarks>
    internal static class ResourceScopePolicyValidator
    {
        // Legacy scope field names, in the deterministic order used when a single mixed-form
        // diagnostic must name the first effective legacy field.
        private const string ScopeType = "scopeType";
        private const string ReadOnlyScopes = "readOnlyScopes";
        private const string Flags = "flags";

        public static void Validate(
            TypeGraphNode node,
            TypePackageValidationOptions options,
            List<TypeValidationDiagnostic> diagnostics)
        {
            var obj = node.ObjectNode;
            var document = node.Document;
            string path = document.PackageRelativePath;
            var sourceMap = document.SourceMap;

            // Collect present, shape-valid legacy scope fields (wrong-shape fields are owned by
            // the structural layer and are skipped here).
            bool hasScopeType = PolicyNodeReader.TryGetIntegerProperty(obj, ScopeType, out var scopeTypeProp, out _);
            bool hasReadOnlyScopes = PolicyNodeReader.TryGetIntegerProperty(obj, ReadOnlyScopes, out var readOnlyScopesProp, out _);
            bool hasFlags = PolicyNodeReader.TryGetIntegerProperty(obj, Flags, out var flagsProp, out long flagsValue);

            if (!hasScopeType && !hasReadOnlyScopes && !hasFlags)
            {
                return;
            }

            bool modernPresent =
                PolicyNodeReader.TryGetIntegerProperty(obj, "readableScopes", out _, out _) ||
                PolicyNodeReader.TryGetIntegerProperty(obj, "writableScopes", out _, out _);

            // An effective legacy scope field is one that a reader would treat as legacy: scopeType
            // and readOnlyScopes always count; flags only counts when it is a non-zero flag value.
            string? firstEffectiveLegacy =
                hasScopeType ? ScopeType :
                hasReadOnlyScopes ? ReadOnlyScopes :
                (hasFlags && flagsValue != 0) ? Flags :
                null;

            if (modernPresent && firstEffectiveLegacy != null)
            {
                var location = node.Location;
                diagnostics.Add(TypeValidationDiagnosticBuilder.ResourceScopeFormMixed(
                    path, node.JsonPointer, firstEffectiveLegacy, location.Line, location.Column));
                return;
            }

            if (hasScopeType)
            {
                ClassifyField(options, diagnostics, path, node.JsonPointer, ScopeType, scopeTypeProp, sourceMap);
            }

            if (hasReadOnlyScopes)
            {
                ClassifyField(options, diagnostics, path, node.JsonPointer, ReadOnlyScopes, readOnlyScopesProp, sourceMap);
            }

            if (hasFlags)
            {
                ClassifyField(options, diagnostics, path, node.JsonPointer, Flags, flagsProp, sourceMap);
            }
        }

        private static void ClassifyField(
            TypePackageValidationOptions options,
            List<TypeValidationDiagnostic> diagnostics,
            string path,
            string nodeJsonPointer,
            string fieldName,
            JsonProperty property,
            SourceMap sourceMap)
        {
            var location = sourceMap.GetLocation(property.NameByteOffset);
            string pointer = nodeJsonPointer + "/" + fieldName;

            if (options.Mode == TypePackageValidationMode.CanonicalWriter)
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.CanonicalScopeFieldViolation(
                    path, pointer, fieldName, location.Line, location.Column));
            }
            else
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.CompatibilityScopeFieldUsed(
                    path, pointer, fieldName, location.Line, location.Column));
            }
        }
    }
}
