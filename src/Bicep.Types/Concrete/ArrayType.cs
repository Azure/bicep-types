// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    public class ArrayType : TypeBase
    {
        [JsonConstructor]
        public ArrayType(ITypeReference itemType)
            => (ItemType) = (itemType);

        public ITypeReference ItemType { get; }
    }
}