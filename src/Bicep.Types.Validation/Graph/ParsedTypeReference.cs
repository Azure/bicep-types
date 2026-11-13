// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Graph
{
    /// <summary>
    /// A structurally valid reference to a type object, parsed from a <c>{"$ref": "..."}</c>
    /// object.  Carries both the target (path + index) and the source location of the
    /// <c>$ref</c> string so graph diagnostics can point back at the referencing site.
    /// </summary>
    internal sealed class ParsedTypeReference
    {
        public ParsedTypeReference(
            string rawText,
            string packageRelativePath,
            int index,
            string sourcePackageRelativePath,
            string sourceJsonPointer,
            SourceLocation location)
        {
            RawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
            PackageRelativePath = packageRelativePath ?? throw new ArgumentNullException(nameof(packageRelativePath));
            Index = index;
            SourcePackageRelativePath = sourcePackageRelativePath ?? throw new ArgumentNullException(nameof(sourcePackageRelativePath));
            SourceJsonPointer = sourceJsonPointer ?? throw new ArgumentNullException(nameof(sourceJsonPointer));
            Location = location;
        }

        /// <summary>The raw <c>$ref</c> string value, e.g. <c>types.json#/3</c>.</summary>
        public string RawText { get; }

        /// <summary>Target file package-relative path; empty string for a same-file reference.</summary>
        public string PackageRelativePath { get; }

        /// <summary>Target type-object index within the target file.</summary>
        public int Index { get; }

        /// <summary>Package-relative path of the file that contains this reference.</summary>
        public string SourcePackageRelativePath { get; }

        /// <summary>JSON pointer of the <c>$ref</c> string within the source file.</summary>
        public string SourceJsonPointer { get; }

        /// <summary>Source location of the <c>$ref</c> string.</summary>
        public SourceLocation Location { get; }

        /// <summary>
        /// Effective target path used for resolution: the source path for same-file
        /// references, otherwise the explicit package path.
        /// </summary>
        public string EffectiveTargetPath =>
            PackageRelativePath.Length == 0 ? SourcePackageRelativePath : PackageRelativePath;
    }
}
