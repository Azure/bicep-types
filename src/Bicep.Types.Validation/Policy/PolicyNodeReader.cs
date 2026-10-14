// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Bicep.Types.Validation.Packaging;

namespace Azure.Bicep.Types.Validation.Policy
{
    /// <summary>
    /// Small helpers for reading raw JSON fields for policy classification.
    /// </summary>
    internal static class PolicyNodeReader
    {
        /// <summary>Finds a direct property by ordinal name, returning the property (with its name offset).</summary>
        public static bool TryGetProperty(JsonValueNode obj, string name, out JsonProperty property)
        {
            foreach (var candidate in obj.Properties)
            {
                if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    property = candidate;
                    return true;
                }
            }

            property = default;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> when a direct property is present and is a shape-valid JSON integer.
        /// When the property is present but has a non-integer shape, this returns <c>false</c> so
        /// policy defers to the structural layer, which owns the primitive-shape diagnostic.
        /// </summary>
        public static bool TryGetIntegerProperty(JsonValueNode obj, string name, out JsonProperty property, out long value)
        {
            if (TryGetProperty(obj, name, out property) &&
                property.Value.Kind == JsonValueKind.Number &&
                property.Value.IsValidInt64)
            {
                value = property.Value.Int64Value;
                return true;
            }

            value = 0;
            return false;
        }
    }
}
