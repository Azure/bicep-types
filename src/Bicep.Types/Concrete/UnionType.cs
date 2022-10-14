// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    public class UnionType : TypeBase
    {
        [JsonConstructor]
        public UnionType(IReadOnlyList<ITypeReference> elements)
            => (Elements) = (elements);

        public IReadOnlyList<ITypeReference> Elements { get; }
    }
}