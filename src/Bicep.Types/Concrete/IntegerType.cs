// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class IntegerType : TypeBase
{
    [JsonConstructor]
    public IntegerType(long? minValue = null, long? maxValue = null)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public long? MinValue { get; }

    public long? MaxValue { get; }
}
