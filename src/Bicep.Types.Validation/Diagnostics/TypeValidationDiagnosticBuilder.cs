// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Diagnostics
{
    /// <summary>
    /// Factory for validation diagnostics with stable codes and messages.
    /// </summary>
    public static class TypeValidationDiagnosticBuilder
    {
        // ── Phase 1 ──────────────────────────────────────────────────────────────

        /// <summary>Builds the deterministic diagnostic returned for archive inputs.</summary>
        public static TypeValidationDiagnostic ArchiveValidationNotImplemented(string displayPath)
        {
            if (displayPath is null) { throw new ArgumentNullException(nameof(displayPath)); }

            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.ArchiveValidationNotImplemented,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Archive package validation is not implemented yet. Provide an extracted package directory or an 'index.json' file instead of archive input '{displayPath}'.");
        }

        // ── Phase 2: input/package-reading ───────────────────────────────────────

        /// <summary>The supplied package path does not point to a valid directory.</summary>
        public static TypeValidationDiagnostic PackagePathInvalid(string displayPath)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.PackagePathInvalid,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"The package path '{displayPath}' does not exist or is not a valid package directory.");
        }

        /// <summary>The package root directory does not contain <c>index.json</c>.</summary>
        public static TypeValidationDiagnostic IndexFileMissing(string displayPath)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.IndexFileMissing,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"The package at '{displayPath}' does not contain an 'index.json' file at the package root.");
        }

        /// <summary>A package JSON file could not be read.</summary>
        public static TypeValidationDiagnostic PackageFileReadFailed(string packageRelativePath, string ioError)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.PackageFileReadFailed,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Failed to read package file '{packageRelativePath}': {ioError}",
                path: packageRelativePath);
        }

        /// <summary>A package JSON file contains a syntax error.</summary>
        public static TypeValidationDiagnostic JsonSyntaxInvalid(string packageRelativePath, int line, int column, string syntaxMessage)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.JsonSyntaxInvalid,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"JSON syntax error in '{packageRelativePath}': {syntaxMessage}",
                path: packageRelativePath,
                line: line,
                column: column);
        }

        // ── Phase 2: structural ───────────────────────────────────────────────────

        /// <summary>The root value of <c>index.json</c> is not a JSON object.</summary>
        public static TypeValidationDiagnostic IndexRootMustBeObject(string packageRelativePath, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.IndexRootMustBeObject,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"The root value of '{packageRelativePath}' must be a JSON object.",
                path: packageRelativePath,
                jsonPointer: string.Empty,
                line: line,
                column: column);
        }

        /// <summary>The root value of a type file is not a JSON array.</summary>
        public static TypeValidationDiagnostic TypeFileRootMustBeArray(string packageRelativePath, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.TypeFileRootMustBeArray,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"The root value of type file '{packageRelativePath}' must be a JSON array.",
                path: packageRelativePath,
                jsonPointer: string.Empty,
                line: line,
                column: column);
        }

        /// <summary>An element in a type-file array is not a JSON object.</summary>
        public static TypeValidationDiagnostic TypeFileElementMustBeObject(string packageRelativePath, string jsonPointer, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.TypeFileElementMustBeObject,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Element '{jsonPointer}' in type file '{packageRelativePath}' must be a JSON object.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        /// <summary>A type object is missing the required <c>$type</c> discriminator.</summary>
        public static TypeValidationDiagnostic TypeObjectDiscriminatorMissing(string packageRelativePath, string jsonPointer, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.TypeObjectDiscriminatorMissing,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Type object at '{jsonPointer}' in '{packageRelativePath}' is missing the required '$type' discriminator field.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        /// <summary>The <c>$type</c> discriminator is present but is not a string.</summary>
        public static TypeValidationDiagnostic TypeObjectDiscriminatorMustBeString(string packageRelativePath, string jsonPointer, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.TypeObjectDiscriminatorMustBeString,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"The '$type' field at '{jsonPointer}' in '{packageRelativePath}' must be a string.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        /// <summary>The <c>$type</c> discriminator names an unsupported type kind.</summary>
        public static TypeValidationDiagnostic TypeObjectDiscriminatorUnsupported(string packageRelativePath, string jsonPointer, string actualDiscriminator, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.TypeObjectDiscriminatorUnsupported,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"The '$type' value '{actualDiscriminator}' at '{jsonPointer}' in '{packageRelativePath}' is not a supported type kind.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        /// <summary>A required property is missing.</summary>
        public static TypeValidationDiagnostic RequiredPropertyMissing(string packageRelativePath, string jsonPointer, string propertyName, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.RequiredPropertyMissing,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Required property '{propertyName}' is missing at '{jsonPointer}' in '{packageRelativePath}'.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        /// <summary>A property has the wrong JSON value type.</summary>
        public static TypeValidationDiagnostic PropertyTypeMismatch(string packageRelativePath, string jsonPointer, string propertyName, string expectedType, string actualType, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.PropertyTypeMismatch,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Property '{propertyName}' at '{jsonPointer}' in '{packageRelativePath}' must be a {expectedType}, but got {actualType}.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        /// <summary>A reference value is not a valid reference object.</summary>
        public static TypeValidationDiagnostic ReferenceObjectInvalid(string packageRelativePath, string jsonPointer, string propertyName, string reason, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.ReferenceObjectInvalid,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Property '{propertyName}' at '{jsonPointer}' in '{packageRelativePath}' must be a reference object ({{\"$ref\": \"...\"}}): {reason}.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        /// <summary>A <c>$ref</c> string does not match the expected syntax.</summary>
        public static TypeValidationDiagnostic ReferenceSyntaxInvalid(string packageRelativePath, string jsonPointer, string refValue, string reason, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.ReferenceSyntaxInvalid,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Reference '{refValue}' at '{jsonPointer}' in '{packageRelativePath}' has invalid syntax: {reason}.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        /// <summary>An unexpected property was found on a JSON object.</summary>
        public static TypeValidationDiagnostic UnknownProperty(string packageRelativePath, string jsonPointer, string propertyName, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.UnknownProperty,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Unexpected property '{propertyName}' at '{jsonPointer}' in '{packageRelativePath}'.",
                path: packageRelativePath,
                jsonPointer: jsonPointer,
                line: line,
                column: column);
        }

        // ── Phase 3: semantic graph ───────────────────────────────────────────────

        /// <summary>A reference targets a type file that does not exist in the package.</summary>
        public static TypeValidationDiagnostic ReferencedTypeFileMissing(
            string sourcePackageRelativePath, string sourceJsonPointer, string targetPackageRelativePath, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.ReferencedTypeFileMissing,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Reference at '{sourceJsonPointer}' in '{sourcePackageRelativePath}' targets missing type file '{targetPackageRelativePath}'.",
                path: sourcePackageRelativePath,
                jsonPointer: sourceJsonPointer,
                line: line,
                column: column);
        }

        /// <summary>
        /// A reference targets a type file that exists but could not be read.  Uses the
        /// <see cref="TypeValidationDiagnosticCodes.PackageFileReadFailed"/> code but, unlike the
        /// reader-time overload, points at the referencing <c>$ref</c> site and names the target file.
        /// </summary>
        public static TypeValidationDiagnostic ReferencedTypeFileReadFailed(
            string sourcePackageRelativePath, string sourceJsonPointer, string targetPackageRelativePath, string ioError, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.PackageFileReadFailed,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Reference at '{sourceJsonPointer}' in '{sourcePackageRelativePath}' targets type file '{targetPackageRelativePath}', which could not be read: {ioError}",
                path: sourcePackageRelativePath,
                jsonPointer: sourceJsonPointer,
                line: line,
                column: column);
        }

        /// <summary>A reference targets a type file that could not be parsed or is not a usable type-file array.</summary>
        public static TypeValidationDiagnostic ReferencedTypeFileUnusable(
            string sourcePackageRelativePath, string sourceJsonPointer, string targetPackageRelativePath, int line, int column,
            IReadOnlyList<TypeValidationDiagnosticRelatedLocation>? relatedLocations = null)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.ReferencedTypeFileUnusable,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Reference at '{sourceJsonPointer}' in '{sourcePackageRelativePath}' targets type file '{targetPackageRelativePath}', which is not a usable type-file array.",
                path: sourcePackageRelativePath,
                jsonPointer: sourceJsonPointer,
                line: line,
                column: column,
                relatedLocations: relatedLocations);
        }

        /// <summary>A reference names a type-object index that is out of range for the target file.</summary>
        public static TypeValidationDiagnostic ReferenceIndexOutOfRange(
            string sourcePackageRelativePath, string sourceJsonPointer, string targetPackageRelativePath, int targetIndex, int targetCount, int line, int column)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.ReferenceIndexOutOfRange,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Reference at '{sourceJsonPointer}' in '{sourcePackageRelativePath}' targets index {targetIndex} in '{targetPackageRelativePath}', but the file contains {targetCount} type objects.",
                path: sourcePackageRelativePath,
                jsonPointer: sourceJsonPointer,
                line: line,
                column: column);
        }

        /// <summary>A top-level <c>index.json</c> root reference resolves to the wrong type-object kind.</summary>
        public static TypeValidationDiagnostic TopLevelTargetKindMismatch(
            string sourcePackageRelativePath, string sourceJsonPointer, string rootDescription, string expectedKinds, string actualKind, int line, int column,
            IReadOnlyList<TypeValidationDiagnosticRelatedLocation>? relatedLocations = null)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.TopLevelTargetKindMismatch,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"{rootDescription} must reference {expectedKinds}, but the target is {actualKind}.",
                path: sourcePackageRelativePath,
                jsonPointer: sourceJsonPointer,
                line: line,
                column: column,
                relatedLocations: relatedLocations);
        }

        /// <summary>A nested type-object reference resolves to a kind not allowed for its role.</summary>
        public static TypeValidationDiagnostic NestedTargetKindMismatch(
            string sourcePackageRelativePath, string sourceJsonPointer, string roleDescription, string expectedKinds, string actualKind, int line, int column,
            IReadOnlyList<TypeValidationDiagnosticRelatedLocation>? relatedLocations = null)
        {
            return new TypeValidationDiagnostic(
                code: TypeValidationDiagnosticCodes.NestedTargetKindMismatch,
                severity: TypeValidationDiagnosticSeverity.Error,
                message: $"Reference at '{sourceJsonPointer}' in '{sourcePackageRelativePath}' for role '{roleDescription}' must target {expectedKinds}, but the target is {actualKind}.",
                path: sourcePackageRelativePath,
                jsonPointer: sourceJsonPointer,
                line: line,
                column: column,
                relatedLocations: relatedLocations);
        }
    }
}
