// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Bicep.Types.Validation.Diagnostics;
using Azure.Bicep.Types.Validation.Graph;
using Azure.Bicep.Types.Validation.Packaging;
using Azure.Bicep.Types.Validation.Policy;
using Azure.Bicep.Types.Validation.Semantic;

namespace Azure.Bicep.Types.Validation.Hygiene
{
    /// <summary>
    /// Strict package-hygiene validation, enabled only when
    /// <see cref="TypePackageValidationOptions.ValidateUnreachableFiles"/> is set.  Every package file
    /// that is not reachable from <c>index.json</c> roots is reported, and unreachable JSON type files
    /// are additionally validated for structural, graph, scalar, and policy defects so latent invalid
    /// content is caught.  The root-reachable closure is never re-validated, keeping diagnostics
    /// duplicate-free.
    /// </summary>
    internal static class PackageHygieneValidator
    {
        public static IReadOnlyList<TypeValidationDiagnostic> Validate(
            IPackageFileSystem fileSystem,
            PackageDocumentProvider provider,
            TypePackageValidationOptions options,
            HashSet<TypeNodeId> visited)
        {
            if (fileSystem == null) { throw new ArgumentNullException(nameof(fileSystem)); }
            if (provider == null) { throw new ArgumentNullException(nameof(provider)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (visited == null) { throw new ArgumentNullException(nameof(visited)); }

            var diagnostics = new List<TypeValidationDiagnostic>();

            var reached = provider.GetReachedFilePaths();
            var files = fileSystem.EnumerateFiles()
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            // Snapshot the provider's load diagnostics so only files newly loaded by strict hygiene
            // contribute structural/parse diagnostics (added exactly once as a delta below).
            int loadDiagnosticsBefore = provider.LoadDiagnostics.Count;

            var seeds = new List<TypeGraphNode>();
            var unreachableUsableFiles = new List<PackageDocumentProviderResult>();

            foreach (var file in files)
            {
                if (reached.Contains(file))
                {
                    continue;
                }

                if (!IsJsonFile(file))
                {
                    diagnostics.Add(TypeValidationDiagnosticBuilder.UnexpectedPackageFile(file));
                    continue;
                }

                diagnostics.Add(TypeValidationDiagnosticBuilder.UnreachablePackageFile(file));

                // Load the unreachable JSON file through the provider so parsing, structural
                // classification, diagnostics, and caching match reachable type-file loading.
                var result = provider.GetTypeFile(file);
                if (result.IsStructurallyUsable)
                {
                    unreachableUsableFiles.Add(result);
                    foreach (var node in result.NodesByIndex)
                    {
                        if (node != null)
                        {
                            seeds.Add(node);
                        }
                    }
                }
            }

            // Validate reference edges from unreachable type objects. Sharing the root-closure
            // visited set prevents re-checking already-validated nodes. Edges from unreachable files
            // resolve through the same graph rules as reachable edges, so this may add BCPVT017,
            // BCPVT018, BCPVT019, or target-kind diagnostics for latent invalid references.
            diagnostics.AddRange(SemanticGraphValidator.ValidateUnreachableSeeds(provider, seeds, visited));

            // Scalar and policy validation over only the newly loaded unreachable files.
            diagnostics.AddRange(ScalarSemanticValidator.Validate(unreachableUsableFiles, options));
            diagnostics.AddRange(PolicyValidator.Validate(unreachableUsableFiles, options));

            // Append structural/parse diagnostics for every file newly loaded during strict hygiene
            // (the unreachable seeds plus anything their edges reached), exactly once.
            for (int i = loadDiagnosticsBefore; i < provider.LoadDiagnostics.Count; i++)
            {
                diagnostics.Add(provider.LoadDiagnostics[i]);
            }

            return diagnostics;
        }

        private static bool IsJsonFile(string packageRelativePath) =>
            packageRelativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }
}
