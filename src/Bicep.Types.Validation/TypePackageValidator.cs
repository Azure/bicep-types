// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Public entry point for validating a Bicep type package.
    /// </summary>
    /// <remarks>
    /// Phase 1 is an input-only driver: directory and raw <c>index.json</c> inputs produce
    /// an empty valid result, and archive inputs produce a deterministic not-implemented
    /// diagnostic. Later phases insert structural, graph, and policy validators between input
    /// resolution and result shaping without changing this call shape.
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

            // Phase 1 intentionally performs no structural, graph, or policy validation.
            // Directory and index inputs therefore produce an empty valid result, while
            // archive inputs already carry the not-implemented diagnostic from resolution.

            return TypePackageValidationResult.Create(effectiveOptions.Mode, diagnostics, effectiveOptions);
        }
    }
}
