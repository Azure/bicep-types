// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Validates the local shape of the <c>index.json</c> document.
    /// Does not resolve or follow type-file references (that belongs to graph validation).
    /// </summary>
    internal static class IndexDocumentValidator
    {
        // Known top-level fields of index.json
        private static readonly string[] KnownTopLevel = new[]
        {
            "resources", "resourceFunctions", "namespaceFunctions", "settings", "fallbackResourceType"
        };

        public static void Validate(JsonShapeReader reader, StructuralValidationContext context)
        {
            var doc = context.CurrentDocument;
            var root = doc.Root;

            // root must be an object
            if (!reader.RequireRootObject(root, out var obj))
            {
                return; // phase gate: wrong root shape, do not inspect children
            }

            // Required top-level fields
            bool hasResources = reader.RequireProperty(obj, string.Empty, "resources", out var resources);
            bool hasResourceFunctions = reader.RequireProperty(obj, string.Empty, "resourceFunctions", out var resourceFunctions);
            bool hasNamespaceFunctions = reader.RequireProperty(obj, string.Empty, "namespaceFunctions", out var namespaceFunctions);

            // resources must be an object map
            if (hasResources)
            {
                ValidateResourcesMap(reader, context, resources);
            }

            // resourceFunctions must be a nested map: resourceType -> apiVersion -> array of refs
            if (hasResourceFunctions)
            {
                ValidateResourceFunctionsMap(reader, context, resourceFunctions);
            }

            // namespaceFunctions must be an array of refs
            if (hasNamespaceFunctions)
            {
                ValidateNamespaceFunctionsArray(reader, context, namespaceFunctions);
            }

            // Optional: settings
            if (obj.TryGetProperty("settings", out var settings))
            {
                ValidateSettings(reader, context, settings);
            }

            // Optional: fallbackResourceType
            if (obj.TryGetProperty("fallbackResourceType", out var fallback))
            {
                ReferenceSyntax.Validate(fallback, "fallbackResourceType", "/fallbackResourceType", context);
            }

            // Unknown top-level fields. index.json has no documented legacy top-level
            // fields, so unknown fields are rejected in both modes.
            var knownSet = new HashSet<string>(KnownTopLevel, StringComparer.Ordinal);
            foreach (var prop in obj.Properties)
            {
                if (!knownSet.Contains(prop.Name))
                {
                    var loc = doc.SourceMap.GetLocation(prop.NameByteOffset);
                    context.Add(TypeValidationDiagnosticBuilder.UnknownProperty(
                        doc.PackageRelativePath, string.Empty, prop.Name, loc.Line, loc.Column));
                }
            }
        }

        private static void ValidateResourcesMap(JsonShapeReader reader, StructuralValidationContext context, JsonValueNode resources)
        {
            if (!reader.RequireObject(resources, string.Empty, "resources"))
            {
                return;
            }

            foreach (var entry in resources.Properties)
            {
                string pointer = "/resources/" + JsonPointerEscape(entry.Name);
                ReferenceSyntax.Validate(entry.Value, entry.Name, pointer, context);
            }
        }

        private static void ValidateResourceFunctionsMap(JsonShapeReader reader, StructuralValidationContext context, JsonValueNode resourceFunctions)
        {
            if (!reader.RequireObject(resourceFunctions, string.Empty, "resourceFunctions"))
            {
                return;
            }

            foreach (var rtEntry in resourceFunctions.Properties)
            {
                string rtPointer = "/resourceFunctions/" + JsonPointerEscape(rtEntry.Name);

                if (rtEntry.Value.Kind != JsonValueKind.Object)
                {
                    var loc = context.CurrentDocument.SourceMap.GetLocation(rtEntry.Value.ByteOffset);
                    context.Add(TypeValidationDiagnosticBuilder.PropertyTypeMismatch(
                        context.CurrentDocument.PackageRelativePath,
                        rtPointer, rtEntry.Name, "object",
                        DescribeKind(rtEntry.Value.Kind),
                        loc.Line, loc.Column));
                    continue;
                }

                foreach (var avEntry in rtEntry.Value.Properties)
                {
                    string avPointer = rtPointer + "/" + JsonPointerEscape(avEntry.Name);

                    if (!reader.RequireArray(avEntry.Value, avPointer, avEntry.Name, out var funcRefs))
                    {
                        continue;
                    }

                    for (int i = 0; i < funcRefs.Count; i++)
                    {
                        ReferenceSyntax.Validate(funcRefs[i], avEntry.Name + "[" + i + "]", avPointer + "/" + i, context);
                    }
                }
            }
        }

        private static void ValidateNamespaceFunctionsArray(JsonShapeReader reader, StructuralValidationContext context, JsonValueNode namespaceFunctions)
        {
            if (!reader.RequireArray(namespaceFunctions, string.Empty, "namespaceFunctions", out var elements))
            {
                return;
            }

            for (int i = 0; i < elements.Count; i++)
            {
                ReferenceSyntax.Validate(elements[i], "namespaceFunctions[" + i + "]", "/namespaceFunctions/" + i, context);
            }
        }

        private static readonly string[] KnownSettingsFields = new[]
        {
            "name", "version", "isSingleton", "isPreview", "isDeprecated", "configurationType"
        };

        private static void ValidateSettings(JsonShapeReader reader, StructuralValidationContext context, JsonValueNode settings)
        {
            if (!reader.RequireObject(settings, string.Empty, "settings"))
            {
                return;
            }

            string pointer = "/settings";

            // Required string fields
            if (reader.RequireProperty(settings, pointer, "name", out var name))
            {
                reader.RequireString(name, pointer + "/name", "name", out _);
            }

            if (reader.RequireProperty(settings, pointer, "version", out var version))
            {
                reader.RequireString(version, pointer + "/version", "version", out _);
            }

            if (reader.RequireProperty(settings, pointer, "isSingleton", out var isSingleton))
            {
                reader.RequireBool(isSingleton, pointer + "/isSingleton", "isSingleton", out _);
            }

            // Optional bool-or-null fields (null accepted as read-side leniency)
            if (settings.TryGetProperty("isPreview", out var isPreview))
            {
                if (isPreview.Kind != JsonValueKind.Null)
                {
                    reader.RequireBool(isPreview, pointer + "/isPreview", "isPreview", out _);
                }
            }

            if (settings.TryGetProperty("isDeprecated", out var isDeprecated))
            {
                if (isDeprecated.Kind != JsonValueKind.Null)
                {
                    reader.RequireBool(isDeprecated, pointer + "/isDeprecated", "isDeprecated", out _);
                }
            }

            // Optional reference field
            if (settings.TryGetProperty("configurationType", out var configurationType))
            {
                ReferenceSyntax.Validate(configurationType, "configurationType", pointer + "/configurationType", context);
            }

            // Unknown fields in settings. No documented legacy settings fields, so
            // unknown fields are rejected in both modes.
            var knownSet = new HashSet<string>(KnownSettingsFields, StringComparer.Ordinal);
            foreach (var prop in settings.Properties)
            {
                if (!knownSet.Contains(prop.Name))
                {
                    var loc = context.CurrentDocument.SourceMap.GetLocation(prop.NameByteOffset);
                    context.Add(TypeValidationDiagnosticBuilder.UnknownProperty(
                        context.CurrentDocument.PackageRelativePath, pointer, prop.Name, loc.Line, loc.Column));
                }
            }
        }

        private static string JsonPointerEscape(string token)
        {
            return token.Replace("~", "~0").Replace("/", "~1");
        }

        private static string DescribeKind(JsonValueKind kind) => JsonValueKindText.Describe(kind);
    }
}
