// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Identifies the serialized Bicep Types package format that validation rules target.
    /// </summary>
    /// <remarks>
    /// Format version is independent of <see cref="TypePackageValidationMode"/>: the version selects
    /// which serialized package format rules apply, while the mode selects canonical-writer versus
    /// compatible-reader policy for that format. <see cref="BicepTypesV1"/> is the current and only
    /// supported format. It must remain the zero value so <c>default(TypePackageFormatVersion)</c>
    /// selects the supported current format; future additions must not reorder or renumber it.
    /// </remarks>
    public enum TypePackageFormatVersion
    {
        /// <summary>The current serialized Bicep Types package format (<c>bicep-types-v1</c>).</summary>
        BicepTypesV1 = 0,
    }

    /// <summary>
    /// Facts about <see cref="TypePackageFormatVersion"/> values used by the validator.
    /// </summary>
    internal static class TypePackageFormatVersionFacts
    {
        /// <summary>Whether the validator can validate packages of the given format version.</summary>
        public static bool IsSupported(TypePackageFormatVersion version) =>
            version == TypePackageFormatVersion.BicepTypesV1;
    }
}
