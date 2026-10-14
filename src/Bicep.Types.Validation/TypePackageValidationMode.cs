// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation
{
    /// <summary>
    /// Selects the validation policy applied to a type package.
    /// </summary>
    public enum TypePackageValidationMode
    {
        /// <summary>
        /// Enforces the canonical serialized form that writers must emit.
        /// </summary>
        CanonicalWriter,

        /// <summary>
        /// Accepts documented legacy forms that readers must continue to tolerate.
        /// </summary>
        CompatibleReader,
    }
}
