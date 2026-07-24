// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Validates the local shape of one type file (an array of type objects).
    /// </summary>
    internal static class TypeFileValidator
    {
        public static void Validate(JsonShapeReader reader, StructuralValidationContext context)
        {
            var doc = context.CurrentDocument;
            var root = doc.Root;
            var sm = doc.SourceMap;
            var path = doc.PackageRelativePath;

            // root must be an array
            if (!reader.RequireRootArray(root, out var elements))
            {
                return; // phase gate
            }

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                string elementPointer = "/" + i;

                // each element must be an object
                if (element.Kind != JsonValueKind.Object)
                {
                    var loc = sm.GetLocation(element.ByteOffset);
                    context.Add(TypeValidationDiagnosticBuilder.TypeFileElementMustBeObject(
                        path, elementPointer, loc.Line, loc.Column));
                    continue; // phase gate: do not inspect non-objects as type objects
                }

                TypeObjectValidator.Validate(element, elementPointer, reader, context);
            }
        }
    }
}
