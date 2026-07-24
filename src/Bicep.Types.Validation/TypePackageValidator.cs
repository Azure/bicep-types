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
    /// The validator reads real package files and runs structural, semantic, graph, and package-hygiene
    /// validation for directory, raw <c>index.json</c>, and gzip-compressed tar archive
    /// (<c>types.tgz</c>) inputs.  Archive inputs are opened as an in-memory package file system so the
    /// same validators run regardless of input form.  The legacy <c>BCPVT001</c> diagnostic is retained
    /// for API stability but is no longer emitted.
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

            // Read package files. Archive inputs are opened as an in-memory package file system;
            // directory and index-file inputs are read from disk. Fatal container/read failures
            // (including malformed archives and unsafe archive members) short-circuit here.
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
                var graphDiagnostics = SemanticGraphValidator.Validate(
                    provider, indexDocument, effectiveOptions, out var visited);
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

                // Phase 6: strict package hygiene. Opt-in; validates files unreachable from
                // index.json roots and reports unreachable/unexpected package members.
                if (effectiveOptions.ValidateUnreachableFiles)
                {
                    var hygieneDiagnostics = Hygiene.PackageHygieneValidator.Validate(
                        readResult.FileSystem, provider, effectiveOptions, visited);
                    diagnostics.AddRange(hygieneDiagnostics);
                }
            }

            return TypePackageValidationResult.Create(effectiveOptions.Mode, diagnostics, effectiveOptions);
        }
    }
}

