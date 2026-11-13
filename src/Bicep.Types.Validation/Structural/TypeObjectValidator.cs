// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Validates one type object (one element of a type-file array).
    /// Checks the <c>$type</c> discriminator, required fields, field shapes, and
    /// unknown properties.  Delegates field-shape checking to
    /// <see cref="TypeShapeCatalog"/> and <see cref="JsonShapeReader"/>.
    /// </summary>
    internal static class TypeObjectValidator
    {
        public static void Validate(
            JsonValueNode obj,
            string jsonPointer,
            JsonShapeReader reader,
            StructuralValidationContext context)
        {
            var doc = context.CurrentDocument;
            var path = doc.PackageRelativePath;
            var sm = doc.SourceMap;

            // $type must be present
            if (!obj.TryGetProperty("$type", out var discriminatorNode))
            {
                var loc = sm.GetLocation(obj.ByteOffset);
                context.Add(TypeValidationDiagnosticBuilder.TypeObjectDiscriminatorMissing(
                    path, jsonPointer, loc.Line, loc.Column));
                return; // phase gate
            }

            // $type must be a string
            if (discriminatorNode.Kind != JsonValueKind.String)
            {
                var loc = sm.GetLocation(discriminatorNode.ByteOffset);
                context.Add(TypeValidationDiagnosticBuilder.TypeObjectDiscriminatorMustBeString(
                    path, jsonPointer + "/$type", loc.Line, loc.Column));
                return; // phase gate
            }

            string discriminator = discriminatorNode.StringValue ?? string.Empty;

            // $type must name a supported kind
            var descriptor = TypeShapeCatalog.GetDescriptor(discriminator);
            if (descriptor == null)
            {
                var loc = sm.GetLocation(discriminatorNode.ByteOffset);
                context.Add(TypeValidationDiagnosticBuilder.TypeObjectDiscriminatorUnsupported(
                    path, jsonPointer + "/$type", discriminator, loc.Line, loc.Column));
                return; // phase gate
            }

            // Validate each described field.
            // knownNames drives the unknown-property check below. Documented legacy fields are
            // known in both modes: the mode-policy layer (phase 4) owns their acceptance or
            // rejection, so structural validation no longer reports them as unknown.
            var knownNames = new List<string>(descriptor.Fields.Count);

            // CompatibleReader narrow relaxation: when a ResourceType uses a legacy scope field,
            // the modern scope pair (readableScopes/writableScopes) is no longer required, mirroring
            // the reader which accepts either the modern pair or a documented legacy form.
            bool relaxModernScopeRequirement =
                !context.IsCanonicalWriter &&
                string.Equals(descriptor.Discriminator, "ResourceType", StringComparison.Ordinal) &&
                (obj.TryGetProperty("scopeType", out _) ||
                 obj.TryGetProperty("readOnlyScopes", out _) ||
                 obj.TryGetProperty("flags", out _));

            foreach (var field in descriptor.Fields)
            {
                knownNames.Add(field.Name);

                bool required = field.Required;
                if (relaxModernScopeRequirement &&
                    (string.Equals(field.Name, "readableScopes", StringComparison.Ordinal) ||
                     string.Equals(field.Name, "writableScopes", StringComparison.Ordinal)))
                {
                    required = false;
                }

                if (required)
                {
                    if (!reader.RequireProperty(obj, jsonPointer, field.Name, out var fieldValue))
                    {
                        continue; // already reported, skip shape check
                    }
                    reader.CheckFieldShape(fieldValue, jsonPointer + "/" + field.Name, field);
                }
                else
                {
                    if (obj.TryGetProperty(field.Name, out var fieldValue))
                    {
                        reader.CheckFieldShape(fieldValue, jsonPointer + "/" + field.Name, field);
                    }
                }
            }

            // Unknown property check. Runs in both modes: genuinely unknown fields are
            // rejected everywhere. Documented legacy fields are always in knownNames and are
            // therefore accepted structurally in both modes (policy classifies them).
            var knownSet = new HashSet<string>(knownNames, StringComparer.Ordinal);
            knownSet.Add("$type");

            foreach (var prop in obj.Properties)
            {
                if (!knownSet.Contains(prop.Name))
                {
                    var loc = sm.GetLocation(prop.NameByteOffset);
                    context.Add(TypeValidationDiagnosticBuilder.UnknownProperty(
                        path, jsonPointer, prop.Name, loc.Line, loc.Column));
                }
            }
        }
    }
}
