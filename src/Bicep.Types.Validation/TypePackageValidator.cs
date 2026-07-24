// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.Structural;

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Public entry point for validating a Bicep type package.
    /// </summary>
    /// <remarks>
    /// Phase 2 adds real package-file reading and structural validation for directory
    /// and raw <c>index.json</c> inputs.  Archive inputs continue to return the phase-1
    /// not-implemented diagnostic.  Later phases will add graph and policy validators
    /// between structural validation and result shaping without changing this call shape.
    /// </remarks>
    public sealed class TypePackageValidator
    {
        /// <summary>Validates the package described by <paramref name="input"/> using default options.</summary>
        public TypePackageValidationResult Validate(TypePackageValidationInput input) =>
            Validate(input, options: null);

        /// <summary>Validates the package described by <paramref name="input"/>.</summary>
        public TypePackageValidationResult Validate(TypePackageValidationInput input, TypePackageValidationOptions? options)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var effectiveOptions = options ?? new TypePackageValidationOptions();

            var diagnostics = new List<TypeValidationDiagnostic>();

            var resolution = PackageInputResolver.Resolve(input);
            diagnostics.AddRange(resolution.Diagnostics);

            // Archive inputs carry the not-implemented diagnostic from resolution; return early.
            if (resolution.Kind == PackageInputKind.ArchiveFile ||
                resolution.Kind == PackageInputKind.ArchiveStream)
            {
                return TypePackageValidationResult.Create(effectiveOptions.Mode, diagnostics, effectiveOptions);
            }

            // Phase 2: read package files
            var readResult = PackageReader.Read(resolution, effectiveOptions);
            diagnostics.AddRange(readResult.Diagnostics);

            if (readResult.HasFatalReadFailure)
            {
                return TypePackageValidationResult.Create(effectiveOptions.Mode, diagnostics, effectiveOptions);
            }

            // Phase 2: structural validation
            var structuralDiagnostics = StructuralValidator.Validate(readResult.Documents, effectiveOptions);
            diagnostics.AddRange(structuralDiagnostics);

            // Phase 3: semantic graph validation. Requires the index document and a package
            // file system; the graph layer loads and structurally validates type files on demand.
            var indexDocument = readResult.Documents.IndexDocument;
            if (indexDocument != null && readResult.FileSystem != null)
            {
                var provider = new PackageDocumentProvider(readResult.FileSystem, indexDocument, effectiveOptions);
                var graphDiagnostics = SemanticGraphValidator.Validate(provider, indexDocument, effectiveOptions);
                diagnostics.AddRange(graphDiagnostics);

                // Phase 5: scalar-semantic validation (value-domain constraints) over the type
                // files graph traversal reached, before mode policy.
                var scalarDiagnostics = Semantic.ScalarSemanticValidator.Validate(
                    provider.GetReachedUsableTypeFiles(), effectiveOptions);
                diagnostics.AddRange(scalarDiagnostics);

                // Phase 4: mode-policy validation over the type files graph traversal reached.
                var policyDiagnostics = Policy.PolicyValidator.Validate(
                    provider.GetReachedUsableTypeFiles(), effectiveOptions);
                diagnostics.AddRange(policyDiagnostics);
            }

            return TypePackageValidationResult.Create(effectiveOptions.Mode, diagnostics, effectiveOptions);
        }
    }
}

