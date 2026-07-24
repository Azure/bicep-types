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
            // knownNames drives the unknown-property check below and is mode-aware:
            // documented legacy fields are known in CompatibleReader but excluded (and
            // therefore reported as unknown) in CanonicalWriter.
            var knownNames = new List<string>(descriptor.Fields.Count);
            foreach (var field in descriptor.Fields)
            {
                // Legacy-compat-only fields: accept in CompatibleReader but treated as unknown in CanonicalWriter
                if (field.LegacyCompatOnly && context.IsCanonicalWriter)
                {
                    // Will be caught by unknown-property check below
                    continue;
                }

                knownNames.Add(field.Name);

                if (field.Required)
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
            // rejected everywhere, while documented legacy fields are only in knownNames
            // (and thus accepted) under CompatibleReader.
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
