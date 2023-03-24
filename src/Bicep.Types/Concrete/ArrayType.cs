// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    public class ArrayType : TypeBase
    {
        [JsonConstructor]
        public ArrayType(ITypeReference itemType, long? minLength = null, long? maxLength = null)
        {
            ItemType = itemType;
            MinLength = minLength;
            MaxLength = maxLength;
        }

        public ITypeReference ItemType { get; }

        public long? MinLength { get; }

        public long? MaxLength { get; }
    }
}
