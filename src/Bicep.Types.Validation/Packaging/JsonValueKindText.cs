// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Produces the stable, baseline-facing textual description of a
    /// <see cref="JsonValueKind"/> used in diagnostic messages.
    /// </summary>
    internal static class JsonValueKindText
    {
        public static string Describe(JsonValueKind kind)
        {
            switch (kind)
            {
                case JsonValueKind.Null: return "null";
                case JsonValueKind.True: return "boolean (true)";
                case JsonValueKind.False: return "boolean (false)";
                case JsonValueKind.Number: return "number";
                case JsonValueKind.String: return "string";
                case JsonValueKind.Array: return "array";
                case JsonValueKind.Object: return "object";
                default: return "unknown";
            }
        }
    }
}
