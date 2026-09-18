// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Azure.Bicep.Types.Validation.Packaging
{
    /// <summary>
    /// Discriminated kind of a JSON value in the lightweight node model.
    /// </summary>
    internal enum JsonValueKind
    {
        Null,
        True,
        False,
        Number,
        String,
        Object,
        Array,
    }

    /// <summary>
    /// One property in a JSON object node, carrying the name, the byte offset of the
    /// name token, and the parsed value.
    /// </summary>
    internal readonly struct JsonProperty
    {
        public readonly string Name;
        public readonly long NameByteOffset;
        public readonly JsonValueNode Value;

        public JsonProperty(string name, long nameByteOffset, JsonValueNode value)
        {
            Name = name;
            NameByteOffset = nameByteOffset;
            Value = value;
        }
    }

    /// <summary>
    /// Lightweight JSON value node built from a single <see cref="System.Text.Json.Utf8JsonReader"/>
    /// pass.  Each node stores its byte offset so callers can convert to source line/column via
    /// <see cref="SourceMap"/> without a second pass.
    /// </summary>
    internal sealed class JsonValueNode
    {
        private static readonly IReadOnlyList<JsonProperty> NoProperties = new JsonProperty[0];
        private static readonly IReadOnlyList<JsonValueNode> NoElements = new JsonValueNode[0];

        private JsonValueNode(
            JsonValueKind kind,
            long byteOffset,
            string? stringValue,
            bool isValidInt64,
            long int64Value,
            IReadOnlyList<JsonProperty>? properties,
            IReadOnlyList<JsonValueNode>? elements)
        {
            Kind = kind;
            ByteOffset = byteOffset;
            StringValue = stringValue;
            IsValidInt64 = isValidInt64;
            Int64Value = int64Value;
            Properties = properties ?? NoProperties;
            Elements = elements ?? NoElements;
        }

        /// <summary>Discriminated kind of this node.</summary>
        public JsonValueKind Kind { get; }

        /// <summary>Byte offset of the token start within the original UTF-8 source.</summary>
        public long ByteOffset { get; }

        /// <summary>Decoded string value; set when <see cref="Kind"/> is <see cref="JsonValueKind.String"/>.</summary>
        public string? StringValue { get; }

        /// <summary>
        /// <c>true</c> when <see cref="Kind"/> is <see cref="JsonValueKind.Number"/> and the value
        /// fits in a 64-bit signed integer.
        /// </summary>
        public bool IsValidInt64 { get; }

        /// <summary>Integer value; valid when <see cref="IsValidInt64"/> is <c>true</c>.</summary>
        public long Int64Value { get; }

        /// <summary>Object properties in declaration order; non-empty only for <see cref="JsonValueKind.Object"/>.</summary>
        public IReadOnlyList<JsonProperty> Properties { get; }

        /// <summary>Array elements in order; non-empty only for <see cref="JsonValueKind.Array"/>.</summary>
        public IReadOnlyList<JsonValueNode> Elements { get; }

        /// <summary>Looks up a property by name using ordinal comparison. Returns <c>false</c> if absent.</summary>
        public bool TryGetProperty(string name, out JsonValueNode value)
        {
            foreach (var prop in Properties)
            {
                if (string.Equals(prop.Name, name, StringComparison.Ordinal))
                {
                    value = prop.Value;
                    return true;
                }
            }
            value = default!;
            return false;
        }

        // ----- Factory methods -----

        internal static JsonValueNode CreateNull(long byteOffset) =>
            new JsonValueNode(JsonValueKind.Null, byteOffset, null, false, 0, null, null);

        internal static JsonValueNode CreateTrue(long byteOffset) =>
            new JsonValueNode(JsonValueKind.True, byteOffset, null, false, 0, null, null);

        internal static JsonValueNode CreateFalse(long byteOffset) =>
            new JsonValueNode(JsonValueKind.False, byteOffset, null, false, 0, null, null);

        internal static JsonValueNode CreateString(long byteOffset, string value) =>
            new JsonValueNode(JsonValueKind.String, byteOffset, value, false, 0, null, null);

        internal static JsonValueNode CreateNumber(long byteOffset, bool isValidInt64, long int64Value) =>
            new JsonValueNode(JsonValueKind.Number, byteOffset, null, isValidInt64, int64Value, null, null);

        internal static JsonValueNode CreateObject(long byteOffset, List<JsonProperty> properties) =>
            new JsonValueNode(JsonValueKind.Object, byteOffset, null, false, 0, properties, null);

        internal static JsonValueNode CreateArray(long byteOffset, List<JsonValueNode> elements) =>
            new JsonValueNode(JsonValueKind.Array, byteOffset, null, false, 0, null, elements);
    }
}
