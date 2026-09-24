// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Structural
{
    /// <summary>
    /// Mutable context object threaded through all structural validators during a single
    /// validation run.  Accumulates diagnostics and provides read access to the current
    /// document and validation options.
    /// </summary>
    internal sealed class StructuralValidationContext
    {
        private readonly List<TypeValidationDiagnostic> diagnostics = new List<TypeValidationDiagnostic>();
        private PackageDocument currentDocument = null!;

        public StructuralValidationContext(TypePackageValidationOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>Validation options for this run.</summary>
        public TypePackageValidationOptions Options { get; }

        /// <summary>Active validation mode.</summary>
        public TypePackageValidationMode Mode => Options.Mode;

        /// <summary><c>true</c> when the mode is <see cref="TypePackageValidationMode.CanonicalWriter"/>.</summary>
        public bool IsCanonicalWriter => Options.Mode == TypePackageValidationMode.CanonicalWriter;

        /// <summary>The document currently being validated.</summary>
        public PackageDocument CurrentDocument => currentDocument;

        /// <summary>Sets the document currently being validated.</summary>
        public void SetCurrentDocument(PackageDocument document)
        {
            currentDocument = document ?? throw new ArgumentNullException(nameof(document));
        }

        /// <summary>Adds a diagnostic to the collection.</summary>
        public void Add(TypeValidationDiagnostic diagnostic)
        {
            if (diagnostic != null)
            {
                diagnostics.Add(diagnostic);
            }
        }

        /// <summary>Returns all accumulated diagnostics.</summary>
        public IReadOnlyList<TypeValidationDiagnostic> GetDiagnostics()
        {
            return diagnostics;
        }
    }
}
