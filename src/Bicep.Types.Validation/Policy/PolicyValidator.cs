// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Policy
{
    /// <summary>
    /// Mode-policy layer.  Runs after structural and semantic-graph validation and classifies
    /// documented legacy serialized forms: it rejects them in <c>CanonicalWriter</c> and accepts
    /// them with warnings in <c>CompatibleReader</c>.  Policy reads the raw JSON node model
    /// (never the deserialized type model) so the original source forms remain visible.
    /// </summary>
    internal static class PolicyValidator
    {
        public static IReadOnlyList<TypeValidationDiagnostic> Validate(
            IEnumerable<PackageDocumentProviderResult> reachedTypeFiles,
            TypePackageValidationOptions options)
        {
            if (reachedTypeFiles == null) { throw new ArgumentNullException(nameof(reachedTypeFiles)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }

            var diagnostics = new List<TypeValidationDiagnostic>();

            foreach (var file in reachedTypeFiles)
            {
                // Inspect every structurally usable element of a reached type file, including
                // elements at indices no reference points to. Non-usable elements (null) were
                // already reported by the structural layer and are skipped here.
                foreach (var node in file.NodesByIndex)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    switch (node.Discriminator)
                    {
                        case "ResourceType":
                            ResourceScopePolicyValidator.Validate(node, options, diagnostics);
                            break;

                        case "BuiltInType":
                            BuiltInTypePolicyValidator.Validate(node, options, diagnostics);
                            break;
                    }
                }
            }

            return diagnostics;
        }
    }
}
