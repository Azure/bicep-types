// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class StringType : TypeBase
{
    [JsonConstructor]
    public StringType(bool? sensitive = null, long? minLength = null, long? maxLength = null, string? pattern = null)
    {
        Sensitive = sensitive;
        MinLength = minLength;
        MaxLength = maxLength;
        Pattern = pattern;
    }

    public bool? Sensitive { get; }

    public long? MinLength { get; }

    public long? MaxLength { get; }

    public string? Pattern { get; }
}
