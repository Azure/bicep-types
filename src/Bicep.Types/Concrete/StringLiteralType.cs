// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    public class StringLiteralType : TypeBase
    {
        [JsonConstructor]
        public StringLiteralType(string value)
            => (Value) = (value);

        public string Value { get; }
    }
}