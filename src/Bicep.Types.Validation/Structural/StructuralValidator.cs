// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Coordinates structural validation of a <see cref="JsonDocumentSet"/>.
    /// Validates the index document, then each type-file document.
    /// Phase gates prevent cascading diagnostics when a document is unusable.
    /// </summary>
    internal static class StructuralValidator
    {
        public static IReadOnlyList<TypeValidationDiagnostic> Validate(
            JsonDocumentSet documents,
            TypePackageValidationOptions options)
        {
            if (documents == null) { throw new ArgumentNullException(nameof(documents)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }

            var context = new StructuralValidationContext(options);
            var reader = new JsonShapeReader(context);

            // Phase gate: index document must be available and structurally usable
            if (documents.IndexDocument == null)
            {
                return context.GetDiagnostics();
            }

            context.SetCurrentDocument(documents.IndexDocument);
            IndexDocumentValidator.Validate(reader, context);

            // Phase gate: if index root was not an object, skip type-file validation
            if (documents.IndexDocument.Root.Kind != JsonValueKind.Object)
            {
                return context.GetDiagnostics();
            }

            // Validate each type file
            foreach (var typeFile in documents.TypeFiles)
            {
                context.SetCurrentDocument(typeFile);
                TypeFileValidator.Validate(reader, context);
            }

            return context.GetDiagnostics();
        }

        /// <summary>
        /// Structurally validates a single type-file document in isolation and returns its
        /// diagnostics.  Used by the graph layer, which loads type files on demand and needs
        /// each file validated exactly once as it is discovered.
        /// </summary>
        public static IReadOnlyList<TypeValidationDiagnostic> ValidateTypeFileDocument(
            PackageDocument typeFile,
            TypePackageValidationOptions options)
        {
            if (typeFile == null) { throw new ArgumentNullException(nameof(typeFile)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }

            var context = new StructuralValidationContext(options);
            var reader = new JsonShapeReader(context);

            context.SetCurrentDocument(typeFile);
            TypeFileValidator.Validate(reader, context);

            return context.GetDiagnostics();
        }
    }
}
