// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class NullType : TypeBase
{
    [JsonConstructor]
    public NullType() {}
}
