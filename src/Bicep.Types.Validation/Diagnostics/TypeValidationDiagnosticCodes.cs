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
        /// <summary>The package root directory does not contain <c>index.json</c>.</summary>
        public const string IndexFileMissing = "BCPVT001";

        /// <summary>A package JSON file is not syntactically valid JSON.</summary>
        public const string JsonSyntaxInvalid = "BCPVT002";

        /// <summary>The root value of <c>index.json</c> is not a JSON object.</summary>
        public const string IndexRootMustBeObject = "BCPVT003";

        /// <summary>The root value of a type file is not a JSON array.</summary>
        public const string TypeFileRootMustBeArray = "BCPVT004";

        /// <summary>An element in a type-file array is not a JSON object.</summary>
        public const string TypeFileElementMustBeObject = "BCPVT005";

        /// <summary>A type object does not contain a <c>$type</c> discriminator field.</summary>
        public const string TypeObjectDiscriminatorMissing = "BCPVT006";

        /// <summary>The <c>$type</c> discriminator field is not a string.</summary>
        public const string TypeObjectDiscriminatorMustBeString = "BCPVT007";

        /// <summary>The <c>$type</c> discriminator names a type kind that is not supported.</summary>
        public const string TypeObjectDiscriminatorUnsupported = "BCPVT008";

        /// <summary>A required property is missing from the JSON object.</summary>
        public const string RequiredPropertyMissing = "BCPVT009";

        /// <summary>A property has the wrong JSON value type (e.g. string where integer expected).</summary>
        public const string PropertyTypeMismatch = "BCPVT010";

        /// <summary>A reference value is not a well-formed reference object.</summary>
        public const string ReferenceObjectInvalid = "BCPVT011";

        /// <summary>A reference string does not match the expected <c>path#/index</c> syntax.</summary>
        public const string ReferenceSyntaxInvalid = "BCPVT012";

        /// <summary>An unexpected property was found on a JSON object.</summary>
        public const string UnknownProperty = "BCPVT013";

        /// <summary>A package file named by a reference could not be read.</summary>
        public const string PackageFileReadFailed = "BCPVT014";

        /// <summary>The supplied package path does not point to a valid directory or file.</summary>
        public const string PackagePathInvalid = "BCPVT015";

        // ── Phase 3: semantic graph ──────────────────────────────────────────────

        /// <summary>A reference targets a type file that does not exist in the package.</summary>
        public const string ReferencedTypeFileMissing = "BCPVT016";

        /// <summary>A reference targets a type file that could not be parsed or is not a usable type-file array.</summary>
        public const string ReferencedTypeFileUnusable = "BCPVT017";

        /// <summary>A reference names a type-object index that is out of range for the target file.</summary>
        public const string ReferenceIndexOutOfRange = "BCPVT018";

        /// <summary>A top-level <c>index.json</c> root reference resolves to the wrong type-object kind.</summary>
        public const string TopLevelTargetKindMismatch = "BCPVT019";

        /// <summary>A nested type-object reference resolves to a kind not allowed for its role.</summary>
        public const string NestedTargetKindMismatch = "BCPVT020";

        // ── Phase 4: mode policy ─────────────────────────────────────────────────

        /// <summary>The package uses a documented legacy form that canonical writers must not emit.</summary>
        public const string CanonicalFormViolation = "BCPVT021";

        /// <summary>A compatible reader accepted a documented legacy form (warning).</summary>
        public const string CompatibilityFormUsed = "BCPVT022";

        /// <summary>A <c>ResourceType</c> mixes modern scope fields with effective legacy scope fields.</summary>
        public const string ResourceScopeFormMixed = "BCPVT023";

        /// <summary><c>BuiltInType.kind</c> is outside the documented serialized enum range.</summary>
        public const string BuiltInTypeKindInvalid = "BCPVT024";

        // ── Phase 5: semantic constraints ────────────────────────────────────────

        /// <summary>A numeric range constraint has its minimum greater than its maximum.</summary>
        public const string NumericRangeInvalid = "BCPVT025";

        /// <summary>A length constraint (<c>minLength</c>/<c>maxLength</c>) is negative.</summary>
        public const string LengthConstraintNegative = "BCPVT026";

        /// <summary>An enum-valued field is outside its documented value set for this validator version.</summary>
        public const string EnumValueInvalid = "BCPVT027";

        /// <summary>A flags-valued field contains bits outside its known mask for this validator version.</summary>
        public const string FlagsValueInvalid = "BCPVT028";

        // ── Phase 6: archive inputs and strict package hygiene ───────────────────

        /// <summary>Archive bytes cannot be read as a valid gzip/tar package (fatal container failure).</summary>
        public const string ArchivePackageInvalid = "BCPVT029";

        /// <summary>An archive member has an invalid package-relative path or an unsupported entry type.</summary>
        public const string ArchiveMemberPathInvalid = "BCPVT030";

        /// <summary>The archive contains the same canonical package-relative file path more than once.</summary>
        public const string ArchiveMemberDuplicate = "BCPVT031";

        /// <summary>Two distinct archive member names collide after canonical path normalization.</summary>
        public const string ArchiveMemberPathCollision = "BCPVT032";

        /// <summary>A package file is not reachable from <c>index.json</c> roots under strict hygiene validation.</summary>
        public const string UnreachablePackageFile = "BCPVT033";

        /// <summary>A strict package scan found an unsupported package member, such as a non-JSON regular file.</summary>
        public const string UnexpectedPackageFile = "BCPVT034";

        // ── Phase 7: format version awareness ────────────────────────────────────

        /// <summary>The selected package format version is not supported by this validator.</summary>
        public const string UnsupportedFormatVersion = "BCPVT035";
    }
}
