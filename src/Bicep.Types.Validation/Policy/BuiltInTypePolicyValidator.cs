// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Policy
{
    /// <summary>
    /// Policy for <c>BuiltInType</c>: canonical writers must not emit any documented built-in
    /// kind (they must use the concrete replacement type), while compatible readers accept them
    /// with a warning.  <c>kind</c> values outside the documented range are errors in both modes.
    /// </summary>
    internal static class BuiltInTypePolicyValidator
    {
        // Documented BuiltInTypeKind serialized values and their canonical replacement types.
        // Kind 8 (ResourceRef) is a reserved legacy form with no canonical replacement.
        private static readonly Dictionary<long, (string Name, string? Replacement)> DocumentedKinds =
            new Dictionary<long, (string, string?)>
            {
                [1] = ("Any", "AnyType"),
                [2] = ("Null", "NullType"),
                [3] = ("Bool", "BooleanType"),
                [4] = ("Int", "IntegerType"),
                [5] = ("String", "StringType"),
                [6] = ("Object", "ObjectType"),
                [7] = ("Array", "ArrayType"),
                [8] = ("ResourceRef", null),
            };

        public static void Validate(
            TypeGraphNode node,
            TypePackageValidationOptions options,
            List<TypeValidationDiagnostic> diagnostics)
        {
            // A missing or non-integer 'kind' is owned by the structural layer (BCPVT010/BCPVT011).
            if (!PolicyNodeReader.TryGetIntegerProperty(node.ObjectNode, "kind", out var kindProperty, out long kind))
            {
                return;
            }

            var document = node.Document;
            var location = document.SourceMap.GetLocation(kindProperty.NameByteOffset);
            string path = document.PackageRelativePath;
            string pointer = node.JsonPointer + "/kind";

            if (!DocumentedKinds.TryGetValue(kind, out var info))
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.BuiltInTypeKindInvalid(
                    path, pointer, kind, location.Line, location.Column));
                return;
            }

            if (options.Mode == TypePackageValidationMode.CanonicalWriter)
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.CanonicalBuiltInTypeViolation(
                    path, pointer, kind, info.Name, info.Replacement, location.Line, location.Column));
            }
            else
            {
                diagnostics.Add(TypeValidationDiagnosticBuilder.CompatibilityBuiltInTypeUsed(
                    path, pointer, kind, info.Name, info.Replacement, location.Line, location.Column));
            }
        }
    }
}
