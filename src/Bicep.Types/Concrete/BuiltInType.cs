// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete
{
    public enum BuiltInTypeKind
    {
        [Obsolete("Please use Azure.Bicep.Types.Concrete.AnyType instead", false)] Any = 1,
        [Obsolete("Please use Azure.Bicep.Types.Concrete.NullType instead", false)] Null = 2,
        [Obsolete("Please use Azure.Bicep.Types.Concrete.BooleanType instead", false)] Bool = 3,
        [Obsolete("Please use Azure.Bicep.Types.Concrete.IntegerType instead", false)] Int = 4,
        [Obsolete("Please use Azure.Bicep.Types.Concrete.StringType instead", false)] String = 5,
        [Obsolete("Please use Azure.Bicep.Types.Concrete.ObjectType instead", false)] Object = 6,
        [Obsolete("Please use Azure.Bicep.Types.Concrete.ArrayType instead", false)] Array = 7,
        [Obsolete("This type kind is no longer in use", false)] ResourceRef = 8,
    }

    public class BuiltInType : TypeBase
    {
        [JsonConstructor]
        public BuiltInType(BuiltInTypeKind kind)
            => (Kind) = (kind);

        public BuiltInTypeKind Kind { get; }
    }
}
