// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Validates Bicep type packages.
    /// </summary>
    public interface ITypePackageValidator
    {
        /// <summary>Validates the package described by <paramref name="input"/> using default options.</summary>
        TypePackageValidationResult Validate(TypePackageValidationInput input);

        /// <summary>Validates the package described by <paramref name="input"/>.</summary>
        TypePackageValidationResult Validate(TypePackageValidationInput input, TypePackageValidationOptions? options);
    }
}