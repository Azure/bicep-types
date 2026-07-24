// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Diagnostics
{
    /// <summary>
    /// Stable diagnostic code constants. Codes are plain strings and form the
    /// user-facing and baseline-facing contract for validation diagnostics.
    /// </summary>
    public static class TypeValidationDiagnosticCodes
    {
        /// <summary>Reported when an archive input is supplied but archive extraction is not implemented.</summary>
        public const string ArchiveValidationNotImplemented = "BCPVT001";

        /// <summary>The package root directory does not contain <c>index.json</c>.</summary>
        public const string IndexFileMissing = "BCPVT002";

        /// <summary>A package JSON file is not syntactically valid JSON.</summary>
        public const string JsonSyntaxInvalid = "BCPVT003";

        /// <summary>The root value of <c>index.json</c> is not a JSON object.</summary>
        public const string IndexRootMustBeObject = "BCPVT004";

        /// <summary>The root value of a type file is not a JSON array.</summary>
        public const string TypeFileRootMustBeArray = "BCPVT005";

        /// <summary>An element in a type-file array is not a JSON object.</summary>
        public const string TypeFileElementMustBeObject = "BCPVT006";

        /// <summary>A type object does not contain a <c>$type</c> discriminator field.</summary>
        public const string TypeObjectDiscriminatorMissing = "BCPVT007";

        /// <summary>The <c>$type</c> discriminator field is not a string.</summary>
        public const string TypeObjectDiscriminatorMustBeString = "BCPVT008";

        /// <summary>The <c>$type</c> discriminator names a type kind that is not supported.</summary>
        public const string TypeObjectDiscriminatorUnsupported = "BCPVT009";

        /// <summary>A required property is missing from the JSON object.</summary>
        public const string RequiredPropertyMissing = "BCPVT010";

        /// <summary>A property has the wrong JSON value type (e.g. string where integer expected).</summary>
        public const string PropertyTypeMismatch = "BCPVT011";

        /// <summary>A reference value is not a well-formed reference object.</summary>
        public const string ReferenceObjectInvalid = "BCPVT012";

        /// <summary>A reference string does not match the expected <c>path#/index</c> syntax.</summary>
        public const string ReferenceSyntaxInvalid = "BCPVT013";

        /// <summary>An unexpected property was found on a JSON object.</summary>
        public const string UnknownProperty = "BCPVT014";

        /// <summary>A package file named by a reference could not be read.</summary>
        public const string PackageFileReadFailed = "BCPVT015";

        /// <summary>The supplied package path does not point to a valid directory or file.</summary>
        public const string PackagePathInvalid = "BCPVT016";

        // ── Phase 3: semantic graph ──────────────────────────────────────────────

        /// <summary>A reference targets a type file that does not exist in the package.</summary>
        public const string ReferencedTypeFileMissing = "BCPVT017";

        /// <summary>A reference targets a type file that could not be parsed or is not a usable type-file array.</summary>
        public const string ReferencedTypeFileUnusable = "BCPVT018";

        /// <summary>A reference names a type-object index that is out of range for the target file.</summary>
        public const string ReferenceIndexOutOfRange = "BCPVT019";

        /// <summary>A top-level <c>index.json</c> root reference resolves to the wrong type-object kind.</summary>
        public const string TopLevelTargetKindMismatch = "BCPVT020";

        /// <summary>A nested type-object reference resolves to a kind not allowed for its role.</summary>
        public const string NestedTargetKindMismatch = "BCPVT021";
    }
}
